using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusLib.Protocols
{
    class ModbusCodeFileRecodWrite
        : ModbusCommandCodec
    {
        #region Client codec

        public override void ClientEncode(
            ModbusCommand command,
            ByteArrayWriter body)
        {
            //数据长度，不算CRC
            int dataLength = command.FileData.Length /  sizeof(ushort);
            int mod = command.FileData.Length % sizeof(ushort);

            int pduDataLength = (dataLength + mod) * 2 + 4 + 1 + 1;
            //1
            body.WriteByte((byte)pduDataLength);

            //数据引用 1
            body.WriteByte(6);

            //文件号 2
            body.WriteUInt16BE(command.FileNo);
            //记录号 2
            body.WriteUInt16BE(command.RecordNo);

            //数据长度  (dataLength + mod) * 2
            body.WriteUInt16BE((ushort)(dataLength + mod));
            Buffer.BlockCopy(command.FileData, 0, command.Data, 0, command.FileData.Length);
            for(int i = 0; i < dataLength + mod; i++)
            {
                body.WriteUInt16BE(command.Data[i]);
            }

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
