using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageStreamWpf.ViewModels.Base
{
  class ViewModelBase : INotifyPropertyChanged, IDisposable
  {
    /// <summary>
    /// 
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Releases resources
    /// </summary>
    public virtual void Dispose()
    {
      
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyName"></param>
    public void OnPropertyChanged(string propertyName)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
  }
}
