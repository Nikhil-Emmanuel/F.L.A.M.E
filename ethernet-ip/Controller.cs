using System;
using System.Threading.Tasks;

namespace ethernetip
{
    public class Controller
    {
        public string IpAddress { get; private set; }
        public int Slot { get; private set; }
        public bool IsConnected { get; private set; }

        public event EventHandler<TagChangedEventArgs>? TagChanged;

        public Controller() {}

        public async Task ConnectAsync(string ipAddress, int slot)
        {
            IpAddress = ipAddress;
            Slot = slot;
            // Simulate connect delay
            await Task.Delay(100);
            IsConnected = true;
        }

        public async Task ReadTagAsync(Tag tag)
        {
            // Simulate reading tag
            await Task.Delay(50);
            tag.Value = 123; // dummy value
            TagChanged?.Invoke(this, new TagChangedEventArgs(tag));
        }
    }

    public class TagChangedEventArgs : EventArgs
    {
        public Tag Tag { get; }
        public TagChangedEventArgs(Tag tag) => Tag = tag;
    }
}