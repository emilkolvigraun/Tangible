using System;
namespace TangibleNode
{
    class DefaultHandler : IResponseHandler
    {
        public void OnResponse(string receiverID, RequestBatch sender, string response)
        {
            if(response==null||receiverID==null) return;
            Response r0 = Encoder.DecodeResponse(response);
            if (r0==null||r0.Status==null) return;

            sender.Batch.ForEach((r) => {
                if (r0.Status.ContainsKey(r.ID) && r0.Status[r.ID])
                {
                    StateLog.Instance.Peers.AccessHeartbeat(receiverID, (i) => i.Reset());
                    StateLog.Instance.RemoveBatchBehind(receiverID, r);
                } else 
                { 
                    StateLog.Instance.AddRequestBehind(receiverID, r);
                }
            });
            if (sender.Completed!=null) sender.Completed.ForEach((a) => {
                StateLog.Instance.Leader_RemoveActionsCompleted(receiverID, a);
            });

            try 
            {
                if (r0.Completed!=null) 
                    r0.Completed.ForEach((a)=>{
                            StateLog.Instance.Leader_AddActionCompleted(a);
                    });

            } catch (Exception e) {Logger.Write(Logger.Tag.ERROR, e.ToString());}
        }
    }
}