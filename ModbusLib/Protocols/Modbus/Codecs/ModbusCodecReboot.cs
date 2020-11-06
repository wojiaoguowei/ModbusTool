using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusLib.Protocols
{
    class ModbusCodecReboot
        : ModbusCommandCodec
    {
        #region Client codec


        public override void ClientEncode(
            ModbusCommand command,
            ByteArrayWriter body)
        {
            //为了代码的统一，子功能码算在body里
            //reboot子功能码默认是00
            body.WriteInt16BE(0);
        }


        public override void ClientDecode(
            ModbusCommand command,
            ByteArrayReader body)
        {

        }

        #endregion
    }
}
