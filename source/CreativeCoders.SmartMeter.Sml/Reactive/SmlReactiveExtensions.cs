using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CreativeCoders.SmartMeter.Sml.Reactive;

public static class SmlReactiveExtensions
{
    public static IObservable<SmlMessage> SelectSmlMessages(this IObservable<byte[]> observable)
    {
        var messageSubject = new Subject<SmlMessage>();
        
        var smlDataReader = new SmlDataReader();
        
        smlDataReader.AddHandler(msg =>
        {
            if (msg.IsValid())
            {
                messageSubject.OnNext(msg);
            }
        });

        observable.Subscribe(data => smlDataReader.Parse(data));

        return messageSubject;
    }

    public static IObservable<SmlValue> SelectSmlValues(this IObservable<SmlMessage> observable)
    {
        var valueReader = new SmlValueReader();

        return observable
            .SelectMany(msg => valueReader.Read(msg.Data))
            .Where(x => x.Value < int.MaxValue);
    }
}
