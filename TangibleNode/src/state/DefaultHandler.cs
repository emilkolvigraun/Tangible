using System;
using System.Linq;

namespace TangibleNode
{
    class DefaultHandler : IResponseHandler
    {
        public void OnResponse(string receiverID, ProcedureCallBatch sender, string response)
        {
            if (sender.Completed!=null)
            {
                foreach(string a in sender.Completed)
                {
                    StateLog.Instance.Leader_RemoveActionsCompleted(receiverID, a);
                }
            }                

            if(response==null||receiverID==null) return;
            Response r0 = Encoder.DecodeResponse(response);
            if (r0==null||r0.Status==null) return;
            
            try 
            {
                if (r0.Completed!=null) 
                    r0.Completed.ForEach((a)=>{
                            StateLog.Instance.Leader_AddActionCompleted(a, receiverID);
                    });

            } catch (Exception e) {Logger.Write(Logger.Tag.ERROR, e.ToString());}

            sender.Batch.ForEach((r) => {
                if (r0.Status.ContainsKey(r.ID) && r0.Status[r.ID])
                {
                    StateLog.Instance.Nodes.AccessHeartbeat(receiverID, (i) => i.Reset());
                    StateLog.Instance.RemoveBatchBehind(receiverID, r);
                } else 
                { 
                    StateLog.Instance.AddToNodeBehind(receiverID, r);
                }
            });
            Node node;
            bool nb = StateLog.Instance.Nodes.TryGetNode(receiverID, out node);
            if (nb) node.HIEVar = r0.HIEVar;

        }
    }
}