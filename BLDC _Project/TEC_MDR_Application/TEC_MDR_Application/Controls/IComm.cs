using System;

namespace BLDC_Demo.Controls
{
    public interface IComm
    {
        event Action ConnectionLost;

        bool Open();
        void Close();
        byte[] SendRecv(byte[] req);
        bool IsOpen { get; }
        string Name { get; }
    }
}
