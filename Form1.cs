using System;
using System.Windows.Forms;
using FTD2XX_NET;

namespace WindowsFormsApp1
{
    public partial class FT232_XILINX_JTAG : Form
    {
        const string manufacturer_string = "Digilent";
        const string product_string = "Digilent USB Device";

        const Byte addr_manufacturer_string_offset = 0x0E; // Offset of the manufacturer string
        const Byte addr_manufacturer_string_length = 0x0F; // Length of manufacturer string
        const Byte addr_product_string_offset = 0x10; // Offset of the product string
        const Byte addr_product_string_length = 0x11; // Length of product string
        const Byte addr_serial_string_offset = 0x12; // Offset of the serial string
        const Byte addr_serial_string_length = 0x13; // Length of serial string

        public FT232_XILINX_JTAG()
        {
            InitializeComponent();
            init_ftdi();
        }
        FTDI myFtdiDevice = null;
        int device_is_open = 0;
        byte[] dd;


        public int init_ftdi()
        {
            // Create new instance of the FTDI device class
            myFtdiDevice = new FTDI();
            return 0;
        }

        public int open_device(int id)
        {
            UInt32 ftdiDeviceCount = 0;
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;
            if (myFtdiDevice != null)
            {
                // Determine the number of FTDI devices connected to the machine
                ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
                // Check status
                if (ftStatus == FTDI.FT_STATUS.FT_OK)
                {
                    textBox1.AppendText("Number of FTDI devices: " + ftdiDeviceCount.ToString() + Environment.NewLine);
                    //System.Diagnostics.Debug.WriteLine("Number of FTDI devices: " + ftdiDeviceCount.ToString());
                    //System.Diagnostics.Debug.WriteLine("");
                }
                else
                {
                    // Wait for a key press
                    //System.Diagnostics.Debug.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                    textBox1.AppendText("Failed to get number of devices (error " + ftStatus.ToString() + ")" + Environment.NewLine);
                    return -1;
                }

                // If no devices available, return
                if (ftdiDeviceCount == 0)
                {
                    // Wait for a key press
                    //System.Diagnostics.Debug.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                    textBox1.AppendText("no devices found");
                    return -1;
                }

                // Allocate storage for device info list
                FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

                // Populate our device list
                ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    textBox1.AppendText("Failed to get device list (error " + ftStatus.ToString() + ")" + Environment.NewLine);
                    return -1;
                }

                 for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                {
                    textBox1.AppendText("Device Index: " + i.ToString() + Environment.NewLine);
                    textBox1.AppendText("Flags: " + String.Format("{0:x}", ftdiDeviceList[i].Flags) + Environment.NewLine);
                    textBox1.AppendText("Type: " + ftdiDeviceList[i].Type.ToString() + Environment.NewLine);
                    textBox1.AppendText("ID: " + String.Format("{0:x}", ftdiDeviceList[i].ID) + Environment.NewLine);
                    textBox1.AppendText("Location ID: " + String.Format("{0:x}", ftdiDeviceList[i].LocId) + Environment.NewLine);
                    textBox1.AppendText("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString() + Environment.NewLine);
                    textBox1.AppendText("Description: " + ftdiDeviceList[i].Description.ToString() + Environment.NewLine);
                    //textBox1.AppendText("");
                }

                // id
                if (ftdiDeviceCount <= id || id < 0) {
                    id = 0;
                }

                // Open first device in our list by serial number
                ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[id].SerialNumber);
                //ftStatus = myFtdiDevice.OpenByLocation(ftdiDeviceList[0].LocId);
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    textBox1.AppendText("Failed to open device (error " + ftStatus.ToString() + ")" + Environment.NewLine);
                    return -1;
                }

                device_is_open = 1;
                textBox1.AppendText("设备"+ id.ToString() +"打开成功！" + Environment.NewLine);

                return 0;
            }
            return -1;
        }
        private void UInt16ToByte(UInt16[] arrInt16, int nInt16Count, ref Byte[] destByteArr)
        {
            //高字节放在前面，低字节放在后面
            for (int i = 0; i < nInt16Count; i++)
            {
                destByteArr[2 * i + 1] = Convert.ToByte((arrInt16[i] & 0xFF00) >> 8);
                destByteArr[2 * i + 0] = Convert.ToByte((arrInt16[i] & 0x00FF));
            }
        }

        private void ByteToUInt16(Byte[] arrByte, int nByteCount, ref UInt16[] destInt16Arr)
        {
            //按两个字节一个整数解析，前一字节当做整数高位，后一字节当做整数低位
            UInt16 change_info;
            for (int i = 0; i < nByteCount / 2; i++)
            {
                change_info = dd[i * 2 + 1];
                change_info <<= 8;
                change_info |= dd[i * 2];
                destInt16Arr[i] = change_info;
            }
        }

        public void read_eep_info()
        {
            dd = System.IO.File.ReadAllBytes("eep.bin");
        }

        public void write_eep_info(string serial_string)
        {
            if (device_is_open != 1)
                return;

            Byte addr_manufacturer_string = 0xA0; // manufacturer string
            Byte manufacturer_string_length = (Byte)(2 + manufacturer_string.Length*2);

            Byte addr_product_string = (Byte)(addr_manufacturer_string + manufacturer_string_length); // product string
            Byte product_string_length = (Byte)(2 + product_string.Length * 2);

            Byte addr_serial_string = (Byte)(addr_product_string + product_string_length); // serial string
            Byte serial_string_length = (Byte)(2 + serial_string.Length * 2);

            dd[addr_manufacturer_string_offset] = addr_manufacturer_string;
            dd[addr_manufacturer_string_length] = manufacturer_string_length;
            dd[addr_manufacturer_string] = manufacturer_string_length;
            dd[addr_manufacturer_string + 1] = 0x03;
            dd[addr_product_string_offset] = addr_product_string;
            dd[addr_product_string_length] = product_string_length;
            dd[addr_product_string] = product_string_length;
            dd[addr_product_string+1] = 0x03;
            dd[addr_serial_string_offset] = addr_serial_string;
            dd[addr_serial_string_length] = serial_string_length;
            dd[addr_serial_string] = serial_string_length;
            dd[addr_serial_string + 1] = 0x03;

            UInt16[] write_info = new UInt16[128];
            ByteToUInt16(dd, dd.Length, ref write_info);

            for (int i = 0; i < manufacturer_string.Length; i++)
                write_info[addr_manufacturer_string / 2 + 1 + i] = manufacturer_string[i];
            UInt16ToByte(write_info, write_info.Length, ref dd);

            for (int i = 0; i < product_string.Length; i++)
                write_info[addr_product_string/2+1+i] = product_string[i];
            UInt16ToByte(write_info, write_info.Length, ref dd);

            for (int i = 0; (i < serial_string.Length) && (addr_serial_string / 2 + 1 + i < write_info.Length); i++)
                write_info[addr_serial_string/2+1+i] = serial_string[i];
            UInt16ToByte(write_info, write_info.Length, ref dd);

            UInt16 check_num = 0xAAAA;
            for (int i = 0; i < write_info.Length - 1; i++)
            {
                check_num = (UInt16)(write_info[i] ^ check_num);
                check_num = (UInt16)((check_num << 1) | (check_num >> 15));
            }

            if (check_num != write_info[write_info.Length - 1])
            {
                textBox1.AppendText("new check: " + check_num + Environment.NewLine);
                write_info[write_info.Length - 1] = check_num;
            }

            UInt16ToByte(write_info, write_info.Length, ref dd);
            System.IO.File.WriteAllBytes("./eep1.bin", dd);

            for (uint i = 0; i < write_info.Length; i++)
            {
                myFtdiDevice.WriteEEPROMLocation(i, write_info[i]);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string txt = textBox2.Text;
            if (device_is_open != 1)
            {
                textBox1.AppendText("请先打开设备！" + Environment.NewLine);
                return;
            }

            read_eep_info();
            write_eep_info(txt);
            //MessageBox.Show("数据写入成功!");
            textBox1.AppendText("数据写入成功！" + Environment.NewLine);


        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (device_is_open == 1 && myFtdiDevice != null)
            {
                myFtdiDevice.Close();
                device_is_open = 0;
                textBox1.AppendText("关闭设备" + Environment.NewLine);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (device_is_open == 1)
                return;
            open_device(int.Parse(textBox3.Text));
        }

    }
}
