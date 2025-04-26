using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcLib.Interfaces
{
    public interface IFuente_Imagen
    {
        byte[] GetNextImage();
    }
}
