using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusLib.Protocols
{
    public class ModbusCodecSetTime
         : ModbusCommandCodec
    {
        /**
         * 临时
         * 
         * */
        /// <summary>

        /// DateTime时间格式转换为Unix时间戳格式

        /// </summary>

        /// <param name="time"> DateTime时间格式</param>

        /// <returns>Unix时间戳格式  单位：毫秒</returns>

        public static long ConvertToTimestamp(DateTime time)

        {

            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));

            return (long)(time - startTime).TotalSeconds * 1000;

        }

        #region Client codec
        public override void ClientEncode(
            ModbusCommand command,
            ByteArrayWriter body)
        {
            //子功能码
            body.WriteInt16BE(0);
            DateTime time;

            Int64 ticksTmp = ConvertToTimestamp(DateTime.Now);



            body.WriteInt64BE(ticksTmp);
        }

        public override void ClientDecode(
            ModbusCommand command,
            ByteArrayReader body)
        {
            //not used
        }


        #endregion
    }
}
