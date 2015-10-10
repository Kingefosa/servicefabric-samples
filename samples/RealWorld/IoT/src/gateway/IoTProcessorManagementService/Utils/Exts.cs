using IoTProcessorManagement.Clients;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace IoTProcessorManagementService
{
    static class Utils
    {
        public static void ThrowHttpError(params string[] Errors)
        {
            Utils.ThrowHttpError(HttpStatusCode.BadRequest, Errors);
        }

        public static void ThrowHttpError(HttpStatusCode httpStatus, params string[] Errors )
        {
           

            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
            responseMessage.Content = new StringContent(Errors.ToMultiLine(), Encoding.UTF8);
            throw new HttpResponseException(responseMessage);

        }
        public static string ToMultiLine(this string[] array)
        {
            var sb = new StringBuilder();
            foreach (var e in array)
                sb.AppendLine(e);

            return sb.ToString();
        }
      
    }
}
