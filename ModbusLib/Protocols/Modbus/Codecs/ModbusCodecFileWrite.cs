using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusLib.Protocols
{
    class ModbusCodecFileWrite
        : ModbusCommandCodec
    {
        #region Client codec

        public override void ClientEncode(
            ModbusCommand command,
            ByteArrayWriter body)
        {
            if (command.SubFunctionCode == ModbusCommand.SubFuncFileWriteOpen)
            {
                //file open read
                body.WriteInt16BE(command.SubFunctionCode);
                byte[] byteArray = command.FileName;
                int size = byteArray.Length;
                body.WriteBytes(byteArray);

                byte[] tmpArray = new byte[128 - size];
                body.WriteBytes(tmpArray);

                body.WriteInt64BE(command.FileLength);

            }
            else if (command.SubFunctionCode == ModbusCommand.SubFuncFileWriteDone)
            {
                //file write done
                body.WriteInt16BE(command.SubFunctionCode);
                byte[] byteArray = command.FileName;
                int size = byteArray.Length;
                body.WriteBytes(byteArray);

                byte[] tmpArray = new byte[128 - size];
                body.WriteBytes(tmpArray);
            }
            
        }


        public override void ClientDecode(
            ModbusCommand command,
            ByteArrayReader body)
        {
            //not used
            if(command.FunctionCode != ModbusCommand.FuncFile)
            {

                if (command.FunctionCode == 0xC1)
                {
                    byte exceptionCode = body.ReadByte();
                    if (exceptionCode == ModbusCommand.ExceptCodeFileWriteDone)
                    {
                        //文件写完成回复异常
                        
                    }
                }
            }
            

        }

        #endregion
    }
}
