using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTProcessorManagement.Common
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

        public static string GetCombinedExceptionMessage(this AggregateException ae)
        {

            StringBuilder sb = new StringBuilder();
            foreach (var e in ae.InnerExceptions)
                sb.AppendLine(string.Concat("E: ", e.Message));

            return sb.ToString();
        }

        public static string GetCombinedExceptionStackTrace(this AggregateException ae)
        {

            StringBuilder sb = new StringBuilder();
            foreach (var e in ae.InnerExceptions)
                sb.AppendLine(string.Concat("StackTrace: ", e.StackTrace));

            return sb.ToString();
        }
    }
}
