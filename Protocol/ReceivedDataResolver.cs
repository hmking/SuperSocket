using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperSocket.Protocol
{
    public class ReceivedDataResolver<TPackageInfo> : IReceivedDataResolver
        where TPackageInfo : IPackageInfo
    {
        private IPackageHandler<TPackageInfo> m_PackageHandler;

        public IReceiveFilter<TPackageInfo> m_ReceiveFilter;

        private ReceivedData m_ReceivedData;

        public ReceivedDataResolver(IPackageHandler<TPackageInfo> packageHandler)
        {
            m_PackageHandler = packageHandler;
            m_ReceivedData = new ReceivedData();
        }

        private void PushResetData(ArraySegment<byte> raw, int rest)
        {
            var segment = new ArraySegment<byte>(raw.Array, raw.Count - rest, rest);
            m_ReceivedData.Current = segment;
            m_ReceivedData.PackageData.Add(segment);
        }

        public event EventHandler NewReceiveBufferRequired;

        private void FireNewReceiveBufferRequired()
        {
            var handler = NewReceiveBufferRequired;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public virtual void Process(ArraySegment<byte> raw)
        {
            m_ReceivedData.Current = raw;
            m_ReceivedData.PackageData.Add(raw);

            var rest = 0;

            while (true)
            {
                var packageInfo = m_ReceiveFilter.Filter(m_ReceivedData, out rest);

                //Receive continue
                if (packageInfo == null)
                {
                    if (rest > 0)
                    {
                        PushResetData(raw, rest);
                        continue;
                    }

                    //Because the current buffer is cached, so new buffer is required for receiving
                    FireNewReceiveBufferRequired();
                    return;
                }

                m_ReceiveFilter.Reset();

                var nextReceiveFilter = m_ReceiveFilter.NextReceiveFilter;

                if (nextReceiveFilter != null)
                    m_ReceiveFilter = nextReceiveFilter;

                m_PackageHandler.Handle(packageInfo);

                m_ReceivedData.PackageData.Clear();

                if (rest <= 0)
                {
                    m_ReceivedData.Current = new ArraySegment<byte>();
                    FireNewReceiveBufferRequired();
                    return;
                }

                PushResetData(raw, rest);
            }
        }
    }
}
