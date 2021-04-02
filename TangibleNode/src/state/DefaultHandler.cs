
namespace TangibleNode
{
    class DefaultHandler : IResponseHandler
    {
        public void OnResponse(string receiverID, RequestBatch sender, string response)
        {
            Response r0 = Encoder.DecodeResponse(response);

            sender.Batch.ForEach((r) => {
                if (r0.Status.ContainsKey(r.ID) && r0.Status[r.ID])
                {
                    StateLog.Instance.Peers.AccessHeartbeat(receiverID, (i) => i.Reset());
                    StateLog.Instance.RemoveBatchBehind(receiverID, r);
                } 
            });
            if (sender.Completed!=null) sender.Completed.ForEach((a) => {
                StateLog.Instance.Leader_RemoveActionsCompleted(receiverID, a);
            });
            if (r0.Completed!=null) r0.Completed.ForEach((a)=>{
                StateLog.Instance.Leader_AddActionCompleted(a);
            });
        }
    }
}