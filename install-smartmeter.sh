#!/usr/bin/env bash
# install-smartmeter.sh
#
# Downloads the SmartMeter Linux server distribution package from either the
# latest stable GitHub release or the latest successful run of a specified
# GitHub Actions workflow, extracts it and runs the bundled install.sh.
#
# Usage: ./install-smartmeter.sh [--workflows <workflow-file>] [-y|--yes] [-h|--help]

set -euo pipefail

# Repository is intentionally hard-wired: this installer only targets the
# official SmartMeter repository.
readonly REPO="CreativeCodersTeam/SmartMeter"
readonly ARTIFACT_NAME="SmartMeter.Server.Linux"
readonly ARCHIVE_FILE="${ARTIFACT_NAME}.tar.gz"
readonly DEFAULT_BRANCH="main"

# Exit codes
readonly EXIT_USER_ABORT=1
readonly EXIT_MISSING_TOOL=2
readonly EXIT_NOT_FOUND=3
readonly EXIT_DOWNLOAD_FAILED=4
readonly EXIT_INSTALL_FAILED=5

WORKFLOW_FILE=""
USE_WORKFLOW=0
ASSUME_YES=0
TMP_DIR=""

usage() {
    cat <<EOF
Usage: $(basename "$0") [OPTIONS]

Downloads SmartMeter ${ARCHIVE_FILE} from ${REPO} and runs its install.sh.

Options:
  --workflows <file>   Use the latest successful run of the given workflow
                       file (e.g. main.yml) on branch '${DEFAULT_BRANCH}' as
                       artifact source. When omitted, the latest stable
                       GitHub release is used.
  -y, --yes            Skip the interactive confirmation prompt.
  -h, --help           Show this help and exit.

Requires: curl, tar, jq, sudo (and gh authenticated when --workflows is used).
EOF
}

log()  { printf '%s\n' "$*" >&2; }
warn() { printf 'WARN: %s\n' "$*" >&2; }
err()  { printf 'ERROR: %s\n' "$*" >&2; }

cleanup() {
    if [[ -n "${TMP_DIR}" && -d "${TMP_DIR}" ]]; then
        rm -rf -- "${TMP_DIR}"
    fi
}
trap cleanup EXIT

parse_args() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --workflows)
                if [[ $# -lt 2 || -z "${2:-}" || "${2:0:1}" == "-" ]]; then
                    err "--workflows requires a workflow file name (e.g. main.yml)"
                    exit "${EXIT_MISSING_TOOL}"
                fi
                USE_WORKFLOW=1
                WORKFLOW_FILE="$2"
                shift 2
                ;;
            -y|--yes)
                ASSUME_YES=1
                shift
                ;;
            -h|--help)
                usage
                exit 0
                ;;
            *)
                err "Unknown argument: $1"
                usage >&2
                exit "${EXIT_MISSING_TOOL}"
                ;;
        esac
    done
}

check_dependencies() {
    local missing=()
    local tool
    # Tools required in every mode: curl fetches the archive, tar extracts it,
    # jq parses API JSON, sudo runs the privileged install.sh.
    for tool in curl tar jq sudo; do
        if ! command -v "$tool" >/dev/null 2>&1; then
            missing+=("$tool")
        fi
    done
    # gh is only required for workflow-run access: listing runs and
    # downloading workflow artifacts always requires authentication.
    # Public release assets are fetched anonymously via curl.
    if [[ "${USE_WORKFLOW}" -eq 1 ]] && ! command -v gh >/dev/null 2>&1; then
        missing+=("gh")
    fi
    if [[ ${#missing[@]} -gt 0 ]]; then
        err "Missing required tools: ${missing[*]}"
        exit "${EXIT_MISSING_TOOL}"
    fi
    if [[ "${USE_WORKFLOW}" -eq 1 ]] && ! gh auth status >/dev/null 2>&1; then
        err "gh is not authenticated. Run 'gh auth login' first."
        exit "${EXIT_MISSING_TOOL}"
    fi
}

# Locates the latest stable (non-prerelease, non-draft) release via the
# public GitHub REST API (no authentication required for public repos) and
# validates that the expected archive asset is attached.
# Prints the asset's browser_download_url on stdout.
locate_release() {
    local api_url="https://api.github.com/repos/${REPO}/releases/latest"
    local response
    if ! response=$(curl -fsSL \
            -H "Accept: application/vnd.github+json" \
            -H "X-GitHub-Api-Version: 2022-11-28" \
            "${api_url}"); then
        err "Failed to query latest release from ${REPO} (${api_url})."
        exit "${EXIT_NOT_FOUND}"
    fi

    local tag published html_url download_url
    tag=$(printf '%s' "${response}"          | jq -r '.tag_name // ""')
    published=$(printf '%s' "${response}"    | jq -r '.published_at // ""')
    html_url=$(printf '%s' "${response}"     | jq -r '.html_url // ""')
    download_url=$(printf '%s' "${response}" \
        | jq -r --arg name "${ARCHIVE_FILE}" \
            '(.assets // [])[] | select(.name == $name) | .browser_download_url' \
        | head -n 1)

    if [[ -z "${tag}" ]]; then
        err "No stable release found in ${REPO}."
        exit "${EXIT_NOT_FOUND}"
    fi
    if [[ -z "${download_url}" ]]; then
        err "Release ${tag} does not contain asset ${ARCHIVE_FILE}."
        exit "${EXIT_NOT_FOUND}"
    fi

    printf 'Found stable release:\n' >&2
    printf '  Tag:       %s\n' "${tag}" >&2
    printf '  Published: %s\n' "${published}" >&2
    printf '  URL:       %s\n' "${html_url}" >&2
    printf '  Asset:     %s\n' "${ARCHIVE_FILE}" >&2

    printf '%s\n' "${download_url}"
}

# Locates the latest successful run of the specified workflow on the default
# branch. Prints the run's database id.
locate_workflow_run() {
    local fields
    if ! fields=$(gh run list \
            --repo "${REPO}" \
            --workflow "${WORKFLOW_FILE}" \
            --status success \
            --branch "${DEFAULT_BRANCH}" \
            --limit 1 \
            --json databaseId,headSha,displayTitle,createdAt,url \
            --jq '.[0] | [(.databaseId | tostring // ""), (.headSha // ""), (.displayTitle // ""), (.createdAt // ""), (.url // "")] | @tsv' \
            2>/dev/null); then
        err "Failed to query workflow runs for ${WORKFLOW_FILE} in ${REPO}."
        exit "${EXIT_NOT_FOUND}"
    fi

    local id sha title created url
    IFS=$'\t' read -r id sha title created url <<<"${fields}"

    if [[ -z "${id}" ]]; then
        err "No successful run found for workflow '${WORKFLOW_FILE}' on branch '${DEFAULT_BRANCH}'."
        exit "${EXIT_NOT_FOUND}"
    fi

    printf 'Found successful workflow run:\n' >&2
    printf '  Workflow:  %s\n' "${WORKFLOW_FILE}" >&2
    printf '  Run ID:    %s\n' "${id}" >&2
    printf '  Commit:    %s\n' "${sha}" >&2
    printf '  Title:     %s\n' "${title}" >&2
    printf '  Created:   %s\n' "${created}" >&2
    printf '  URL:       %s\n' "${url}" >&2
    printf '  Artifact:  %s\n' "${ARTIFACT_NAME}" >&2

    printf '%s\n' "${id}"
}

# Helper removed: we now use `gh --jq ...` directly, which leverages gh's
# embedded jq and keeps the external dependency list minimal.

confirm() {
    if [[ "${ASSUME_YES}" -eq 1 ]]; then
        return 0
    fi
    local reply=""
    printf 'Continue with download and installation? [y/N] ' >&2
    read -r reply || true
    if [[ "${reply}" != "y" && "${reply}" != "Y" ]]; then
        log "Aborted by user."
        exit "${EXIT_USER_ABORT}"
    fi
}

download_release_asset() {
    local url="$1" dest="$2"
    local target="${dest}/${ARCHIVE_FILE}"
    log "Downloading ${ARCHIVE_FILE} from ${url}..."
    if ! curl -fL --progress-bar -o "${target}" "${url}"; then
        err "Failed to download release asset."
        exit "${EXIT_DOWNLOAD_FAILED}"
    fi
    printf '%s\n' "${target}"
}

download_workflow_artifact() {
    local run_id="$1" dest="$2"
    log "Downloading workflow artifact ${ARTIFACT_NAME} from run ${run_id}..."
    if ! gh run download "${run_id}" \
            --repo "${REPO}" \
            --name "${ARTIFACT_NAME}" \
            --dir "${dest}"; then
        err "Failed to download workflow artifact."
        exit "${EXIT_DOWNLOAD_FAILED}"
    fi
    local archive
    archive=$(find "${dest}" -type f -name "${ARCHIVE_FILE}" -print -quit)
    if [[ -z "${archive}" ]]; then
        err "Artifact ${ARTIFACT_NAME} did not contain ${ARCHIVE_FILE}."
        exit "${EXIT_DOWNLOAD_FAILED}"
    fi
    printf '%s\n' "${archive}"
}

extract_archive() {
    local archive="$1" extract_dir="$2"
    mkdir -p "${extract_dir}"
    log "Extracting $(basename "${archive}")..."
    if ! tar -xzf "${archive}" -C "${extract_dir}"; then
        err "Failed to extract ${archive}."
        exit "${EXIT_DOWNLOAD_FAILED}"
    fi
}

run_installer() {
    local extract_dir="$1"
    local installer="${extract_dir}/install.sh"
    if [[ ! -f "${installer}" ]]; then
        err "install.sh not found in extracted package."
        exit "${EXIT_INSTALL_FAILED}"
    fi
    chmod +x "${installer}"
    log "Running installer with sudo..."
    if ! ( cd "${extract_dir}" && sudo ./install.sh ); then
        err "install.sh failed."
        exit "${EXIT_INSTALL_FAILED}"
    fi
}

main() {
    parse_args "$@"
    check_dependencies

    local source_ref archive_path
    TMP_DIR=$(mktemp -d -t smartmeter-install-XXXXXX)
    local download_dir="${TMP_DIR}/download"
    local extract_dir="${TMP_DIR}/extracted"
    mkdir -p "${download_dir}"

    if [[ "${USE_WORKFLOW}" -eq 1 ]]; then
        source_ref=$(locate_workflow_run)
        confirm
        archive_path=$(download_workflow_artifact "${source_ref}" "${download_dir}")
    else
        source_ref=$(locate_release)
        confirm
        archive_path=$(download_release_asset "${source_ref}" "${download_dir}")
    fi

    extract_archive "${archive_path}" "${extract_dir}"
    run_installer "${extract_dir}"

    log "Installation complete."
}

main "$@"
