using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CreativeCoders.SmartMeter.Sml.Reactive;

public static class SmlReactiveExtensions
{
    public static IObservable<SmlMessage> ReadSmlMessages(this IObservable<byte[]> observable)
    {
        var messageSubject = new Subject<SmlMessage>();
        
        var smlDataReader = new SmlDataReader();
        
        smlDataReader.AddHandler(msg => messageSubject.OnNext(msg));

        observable.Subscribe(data => smlDataReader.Parse(data));

        return messageSubject;
    }

    public static IObservable<SmlValue> ReadSmlValues(this IObservable<SmlMessage> observable)
    {
        var valueReader = new SmlValueReader();

        return observable
            .SelectMany(msg => valueReader.Read(msg.Data));
    }
}
