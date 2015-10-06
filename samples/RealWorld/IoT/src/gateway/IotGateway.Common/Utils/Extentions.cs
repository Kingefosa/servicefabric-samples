using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTGateway.Common
{
    public static class Extentions
    {
        public static Task<Byte[]> ToBytes(this Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            int read, current = 0;
            while ((read = stream.Read(bytes, current, bytes.Length - current)) > 0)
                current += read;

            return Task.FromResult( bytes );
        }
    }
}
