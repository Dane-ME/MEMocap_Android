using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEMocap.Android.Models
{
    public class CameraInfo
    {
        public string CameraId { get; set; }
        public CameraType Type { get; set; }
        public string DisplayName { get; set; }
        public object NativeCameraManager { get; set; }
        public object NativeCharacteristics { get; set; }
    }

    public enum CameraType
    {
        Back,
        Front,
        External,
        Telephoto,
        UltraWide
    }
}
