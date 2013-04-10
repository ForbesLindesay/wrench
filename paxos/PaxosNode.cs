using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Paxos
{
    public class PaxosNode
    {
        private readonly Proposer proposer;
        private readonly Acceptor acceptor;
        private readonly Learner learner;

        private readonly string address;
        private int numberOfNodes;  

        public int NumberOfNodes
        {
            get { return numberOfNodes; }
            set
            {
                learner.TotalAcceptors = value;
                proposer.TotalAcceptors = value;
                numberOfNodes = value;
            }
        }
        public PaxosNode(string Address, int NumberOfNodes)
        {
            address = Address;
            numberOfNodes = NumberOfNodes;

            proposer = new Proposer(Address, NumberOfNodes);
            acceptor = new Acceptor();
            learner = new Learner(NumberOfNodes);
            proposer.Pipe(acceptor).Pipe(proposer);
            acceptor.Pipe(learner);
            proposer.Pipe(learner);

            var serializer = new SerializerStream((message) =>
            {
                var handler = Message;
                if (handler != null)
                {
                    handler(this, message);
                }
            });
            proposer.Pipe(serializer);
            acceptor.Pipe(serializer);
        }

        public Task<string> Propose(string Round, string Value)
        {
            return proposer.Propose(Round, Value);
        }


        public bool TryGetResult(string RoundID, out string Result)
        {
            return learner.TryGetResult(RoundID, out Result);
        }
        public Task<string> GetResult(string RoundID)
        {
            return learner.GetResult(RoundID);
        }

        public event EventHandler<RoundResult> RoundComplete
        {
            add { learner.RoundResult += value; }
            remove { learner.RoundResult -= value; }
        }

        #region Networking
        public event EventHandler<string> Message;
        public void OnMessage(string Message)
        {
            var ser = new JavaScriptSerializer();
            var message = ser.Deserialize<TransportMessage>(Message);
            proposer.SendMessage(message.To, message.From, message.NM);
        }
        #endregion

        #region Inner Classes
        private class TransportMessage
        {
            public string To;
            public string From;
            public NetworkMessage NM;
        }
        private class SerializerStream : IWriteStream<NetworkMessage>
        {
            private readonly Action<string> onMessage;
            public SerializerStream(Action<string> OnMessage)
            {
                onMessage = OnMessage;
            }


            public void SendMessage(string To, string From, NetworkMessage Message)
            {
                var ser = new JavaScriptSerializer();
                var message = ser.Serialize(new TransportMessage()
                {
                    To = To,
                    From = From,
                    NM = Message
                });
                onMessage(message);
            }
        }
        #endregion
    }
}
