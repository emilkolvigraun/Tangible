using Opc.Ua.Client;
namespace EC.MS
{
    interface ISource
    {
        string GetID();

        bool Consume(string endpoint, MonitoredItemNotificationEventHandler responseHandler);
        bool Stop(string reference = null);
    }
}