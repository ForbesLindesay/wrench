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

        private readonly Object locker = new Object();
       
        private int InitializeResponses = 0;
        private readonly TaskCompletionSource<object> Initialized = new TaskCompletionSource<object>();

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
        public string Address { get { return address; } }
        public PaxosNode(string Address, int NumberOfNodes)
        {
            address = Address;
            numberOfNodes = NumberOfNodes;

            proposer = new Proposer(Address, NumberOfNodes);
            learner = new Learner(NumberOfNodes);
            acceptor = new Acceptor(Address, learner);
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

            Initialize();
        }

        private async void Initialize()
        {
            if (NumberOfNodes == 1)
            {
                Initialized.TrySetResult(null);
            }
            else
            {
                await Task.Yield();//allow time for handler to be registered
                var quorum = Math.Floor(NumberOfNodes / 2D) + 1;
                if (quorum == NumberOfNodes) quorum--;
                int responses = 0;
                while (responses < quorum)
                {
                    var handler = this.Message;
                    if (handler != null)
                    {
                        var ser = new JavaScriptSerializer();
                        handler(this, ser.Serialize(new TransportMessage()
                        {
                            To = "BROADCAST",
                            From = Address,
                            NM = NetworkMessage.Initialize()
                        }));
                    }
                    await Task.Delay(5000);
                    lock (locker)
                    {
                        responses = InitializeResponses;
                    }
                }
            }
        }
        public async Task<string> Propose(string Round, string Value)
        {
            await Initialized.Task;
            return await proposer.Propose(Round, Value);
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
        public async void OnMessage(string Message)
        {
            var ser = new JavaScriptSerializer();
            var message = ser.Deserialize<TransportMessage>(Message);
            if (message.NM.Type == MessageType.Initialize)
            {

                var handler = this.Message;
                if (handler != null)
                {
                    handler(this, ser.Serialize(new TransportMessage(){
                        To = message.From,
                        From = Address,
                        NM = message.NM.Recover(learner.GetDataSet())
                    }));
                }
            }
            else if (message.NM.Type == MessageType.Recover)
            {
                learner.Recover(message.NM.DataSet);
                lock (locker)
                {
                    InitializeResponses++;
                    var quorum = Math.Floor(NumberOfNodes / 2D) + 1;
                    if (quorum == NumberOfNodes) quorum--;
                    if (InitializeResponses > quorum)
                    {
                        Initialized.TrySetResult(null);
                    }
                }
            }
            else
            {
                await Initialized.Task;
                proposer.SendMessage(message.To, message.From, message.NM);
                acceptor.SendMessage(message.To, message.From, message.NM);
                learner.SendMessage(message.To, message.From, message.NM);
            }
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
