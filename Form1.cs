using HalconDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;


namespace ImageProcessLib
{
    public partial class Form1 : Form
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private static extern void MemCopy(IntPtr dest, IntPtr src, int count);//字节数组到字节数组的拷贝

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        private unsafe static extern void MemCopy(void* dest, void* src, int count);//字节数组到字节数组的拷贝

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            byte[] src = new byte[2594 * 1944];
            byte[] dst = new byte[2594 * 1944];
            setvalue(ref src, 1);
            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var psrc= handle.AddrOfPinnedObject();

            var handle2 = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var pdst = handle2.AddrOfPinnedObject();
            
            // Marshal.UnsafeAddrOfPinnedArrayElement(src, src.Length);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            sw.Stop();
           
            setvalue(ref src, 2);
            unsafe
            {
                //MemCopy(pdst, psrc, 0);
                //sw.Restart();
                //MemCopy(pdst, psrc, src.Length);
                //sw.Stop();
                //Console.WriteLine(sw.Elapsed.TotalMilliseconds);

                sw.Restart();
                fixed (byte* p1 = &src[0], p2 = &dst[0])
                {
                    MemCopy(new IntPtr( p2), new IntPtr(p1), src.Length);
                }
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalMilliseconds);
                setvalue(ref src, 3);
                sw.Restart();
                fixed (void* p1 = &src[0], p2 = &dst[0])
                {
                    long*p11=(long*)p1;
                    long *p12=(long*)p2;    
                    MemCopy(p12, p11, src.Length);
                }
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalMilliseconds);
                setvalue(ref src, 4);

                sw.Restart();
                MemCopy(pdst, psrc, src.Length);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalMilliseconds);
            }
            handle.Free();
            handle2.Free();
        }

        void setvalue(ref byte[] arr,byte value)
        {
            // return;
            //     for(int i=0;i<arr.Length;i++)
            //    {
            //        arr[i] = value;
            //    }
            //    unsafe
            //    {
            //        int[] numbers = { 10, 20, 30 };
            //        fixed (int* toFirst = &numbers[0], toLast = &numbers[^1])
            //        {
            //            Console.WriteLine(toLast - toFirst);  // output: 2
            //        }
            //    }
        }


        private void Form1_Load(object sender, EventArgs e)
        {

          
        }
    }

}
