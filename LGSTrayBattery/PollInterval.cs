using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGSTrayBattery
{
    public class PollInterval : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int delayTime; //ms
        public string DisplayName { get; private set; }
        public bool IsChecked { get; set; }

        public PollInterval(int delayTime, string displayName)
        {
            this.delayTime = delayTime;
            this.DisplayName = displayName;
        }
    }
}
