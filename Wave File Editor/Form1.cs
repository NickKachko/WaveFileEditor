using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Xml.Serialization;


namespace Wave_File_Editor
{
    public partial class Form1 : Form
    {
        string FileName = "vehicle_rollover2.wav";
        Graphics gPanel;
        Pen MyPen = new Pen(Color.Blue);
        int SamplesPerPixel, HeightPerPixel,PixelsH,PixelsW,StartSample,EndSample,PasteSample;
        Int64 temp;
        bool IsClicked = false, IsPasting=false;
        Point Start, End;
        BinaryWriter wr;
        FileStream f;
        WaveFile file;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            if (openFileDialog1.CheckPathExists)
            {
                FileName = openFileDialog1.FileName;
            }
            textBox1.Text = FileName;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //uint numcuts;
            //float volume;
            //double[] cutpoints;
            //XmlSerializer xmlout;
            //xmlout= new XmlSerializer(typeof(WaveFile));
            //Stream writer = new FileStream(FileName+".wav", FileMode.Create);
            //string temp;

            //We use a custom filereader called WaveFileReader to retrieve the data from the
            //wave files.  In addition to conforming to good coding conventions, this streamlines
            //the code: here we just look at the "big picture", while in the WaveFileReader class
            //we only care what's going on in one small place at a time.
            WaveFileReader adapter = new WaveFileReader(FileName);
            file = new WaveFile();
            file.riffheader = adapter.ReadMainFileHeader();
            file.riffheader.FileName = FileName;
            file.format = adapter.ReadFormatHeader();
            file.data = adapter.ReadDataHeader();
            DrawInfo();

            //xmlout.Serialize(writer, file);
            //textBox1.Text = "Ok";
        }
        private void DrawInfo()
        {
            textBox2.Text += file.WriteHeader();
            textBox2.Text += file.WriteFormat();
            textBox2.Text += file.WriteData();
            gPanel = pictureBox1.CreateGraphics();
            gPanel.Clear(SystemColors.Control);
            SamplesPerPixel = Convert.ToInt32(Math.Ceiling(file.data.NumSamples / 900.0));
            HeightPerPixel = Convert.ToInt32(Math.Ceiling(Math.Pow(2, file.format.BitsPerSample / file.format.NumChannels) / 200.0));
            PixelsW = Convert.ToInt32(file.data.NumSamples / SamplesPerPixel);
            PixelsH = Convert.ToInt32(Math.Pow(2, file.format.BitsPerSample / file.format.NumChannels) / HeightPerPixel);
            textBox1.Text = SamplesPerPixel + " " + HeightPerPixel + " " + PixelsW + " " + PixelsH;
            for (int i = 0; i < PixelsW; i++)
            {
                temp = 0;
                for (int j = 0; j < SamplesPerPixel; j++)
                {
                    temp += file.data.Samples[i * SamplesPerPixel + j].Channels[0].data;
                }
                temp = Convert.ToInt32(temp / Convert.ToDouble(SamplesPerPixel));
                gPanel.DrawLine(MyPen, new Point(i, 100), new Point(i, 100 - Convert.ToInt32(temp / HeightPerPixel)));
            }
        }
        private void RecalculateInfo()
        {
            file.data.Subchunk2Size = Convert.ToUInt32(file.data.Samples.Count * file.format.BitsPerSample / 8);
            file.riffheader.ChunkSize = 36 + file.data.Subchunk2Size;
            file.data.NumSamples = Convert.ToUInt32(file.data.Subchunk2Size / (file.format.BitsPerSample / 8 * file.format.NumChannels));
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (!IsPasting)
            {
                Start = e.Location;
                textBox3.Text = Start.ToString();
                IsClicked = true;
                StartSample = Start.X * SamplesPerPixel;
                textBox5.Text = StartSample.ToString();
            }
            else
            {
                PasteSample = e.X * SamplesPerPixel;
                file.data.Samples.InsertRange(PasteSample, file.data.Samples.GetRange(StartSample, EndSample - StartSample));
                RecalculateInfo();
                DrawInfo();
                IsPasting = false;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsClicked)
            {
                End = e.Location;
                textBox4.Text = End.ToString();
                EndSample = End.X * SamplesPerPixel;
                textBox6.Text = EndSample.ToString();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            IsClicked = false;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                f = new FileStream(FileName + ".wav", FileMode.Create);
                wr = new BinaryWriter(f);
                wr.Write(System.Text.Encoding.ASCII.GetBytes(file.riffheader.ChunkID));
                wr.Write(file.riffheader.ChunkSize);
                wr.Write(System.Text.Encoding.ASCII.GetBytes(file.riffheader.Format));

                wr.Write(System.Text.Encoding.ASCII.GetBytes(file.format.Subchunk1ID));
                wr.Write(file.format.Subchunk1Size);
                wr.Write(file.format.AudioFormat);
                wr.Write(file.format.NumChannels);
                wr.Write(file.format.SampleRate);
                wr.Write(file.format.ByteRate);
                wr.Write(file.format.BlockAlign);
                wr.Write(file.format.BitsPerSample);

                wr.Write(System.Text.Encoding.ASCII.GetBytes(file.data.Subchunk2ID));
                wr.Write(file.data.Subchunk2Size);
                for (int i = 0; i < file.data.Samples.Count; i++)
                {
                    for (int k = 0; k < file.format.NumChannels; k++)
                    {
                        if (file.format.BitsPerSample / file.format.NumChannels == 8)
                            wr.Write(Convert.ToByte(file.data.Samples[i].Channels[k].data));
                        if (file.format.BitsPerSample / file.format.NumChannels == 16)
                            wr.Write(Convert.ToInt16(file.data.Samples[i].Channels[k].data));
                        if (file.format.BitsPerSample / file.format.NumChannels == 32)
                            wr.Write(Convert.ToInt32(file.data.Samples[i].Channels[k].data));
                    }
                }
                wr.Close();
                f.Close();
                button5.Text = "Done";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            file.data.Samples.RemoveRange(StartSample, EndSample - StartSample);
            RecalculateInfo();
            DrawInfo();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            IsPasting = true;
        }
    }
    public class WaveFileReader : IDisposable
    {
        BinaryReader reader;

        riffChunk mainfile;
        fmtChunk format;
        factChunk fact;
        dataChunk data;

        #region General Utilities
        /*
		 * WaveFileReader(string) - 2004 July 28
		 * A fairly standard constructor that opens a file using the filename supplied to it.
		 */
        public WaveFileReader(string filename)
        {
            reader = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        /*
         * long GetPosition() - 2004 July 28
         * Returns the current position of the reader's BaseStream.
         */
        public long GetPosition()
        {
            return reader.BaseStream.Position;
        }

        /*
         * string GetChunkName() - 2004 July 29
         * Reads the next four bytes from the file, converts the 
         * char array into a string, and returns it.
         */
        public string GetChunkName()
        {
            return new string(reader.ReadChars(4));
        }

        /*
         * void AdvanceToNext() - 2004 August 2
         * Advances to the next chunk in the file.  This is fine, 
         * since we only really care about the fmt and data 
         * streams for now.
         */
        public void AdvanceToNext()
        {
            long NextOffset = (long)reader.ReadUInt32(); //Get next chunk offset
            //Seek to the next offset from current position
            reader.BaseStream.Seek(NextOffset, SeekOrigin.Current);
        }
        #endregion
        #region Header Extraction Methods
        /*
		 * WaveFileFormat ReadMainFileHeader - 2004 July 28
		 * Read in the main file header.  Not much more to say, really.
		 * For XML serialization purposes, I "correct" the dwFileLength
		 * field to describe the whole file's length.
		 */
        public riffChunk ReadMainFileHeader()
        {
            mainfile = new riffChunk();

            mainfile.ChunkID = new string(reader.ReadChars(4));
            mainfile.ChunkSize = reader.ReadUInt32();
            mainfile.Format = new string(reader.ReadChars(4));
            return mainfile;
        }

        //fmtChunk ReadFormatHeader() - 2004 July 28
        //Again, not much to say.
        public fmtChunk ReadFormatHeader()
        {
            format = new fmtChunk();

            format.Subchunk1ID = new String(reader.ReadChars(4));
            format.Subchunk1Size = reader.ReadUInt32();
            format.AudioFormat = reader.ReadUInt16();
            format.NumChannels = reader.ReadUInt16();
            format.SampleRate = reader.ReadUInt32();
            format.ByteRate = reader.ReadUInt32();
            format.BlockAlign = reader.ReadUInt16();
            format.BitsPerSample = reader.ReadUInt16();
            return format;
        }

        //factChunk ReadFactHeader() - 2004 July 28
        //Again, not much to say.
        public factChunk ReadFactHeader()
        {
            fact = new factChunk();

            fact.sChunkID = "fact";
            fact.dwChunkSize = reader.ReadUInt32();
            fact.dwNumSamples = reader.ReadUInt32();
            return fact;
        }


        //dataChunk ReadDataHeader() - 2004 July 28
        //Again, not much to say.
        public dataChunk ReadDataHeader()
        {
            try
            {
                data = new dataChunk();

                data.Subchunk2ID = new string(reader.ReadChars(4));
                data.Subchunk2Size = reader.ReadUInt32();
                data.lFilePosition = reader.BaseStream.Position;
                data.NumSamples = Convert.ToUInt32(data.Subchunk2Size / (format.BitsPerSample / 8 * format.NumChannels));
                //The above could be written as data.dwChunkSize / format.wBlockAlign, but I want to emphasize what the frames look like.
                data.dwMinLength = (data.Subchunk2Size / format.ByteRate) / 60;
                data.dSecLength = ((double)data.Subchunk2Size / (double)format.ByteRate) - (double)data.dwMinLength * 60;
                data.Samples = new List<Sample>();
                for(int i=0;i<data.NumSamples;i++)
                {
                    Sample temp = new Sample();
                    temp.Channels = new List<Channel>();
                    for (int j=0;j<format.NumChannels;j++)
                    {
                        if (format.BitsPerSample / format.NumChannels == 8)
                            temp.Channels.Add(new Channel(Convert.ToInt32(reader.ReadByte())));
                        if (format.BitsPerSample / format.NumChannels == 16)
                            temp.Channels.Add(new Channel(Convert.ToInt32(reader.ReadInt16())));
                        if (format.BitsPerSample / format.NumChannels == 32)
                            temp.Channels.Add(new Channel(Convert.ToInt32(reader.ReadInt32())));
                    }
                    data.Samples.Add(temp);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return data;
        }
        #endregion
        #region IDisposable Members

        public void Dispose()
        {
            if (reader != null)
                reader.Close();
        }

        #endregion


    }
    public class riffChunk
    {
        public string FileName;
        //These three fields constitute the riff header
        public string ChunkID;         //RIFF
        public uint ChunkSize;		//In bytes, measured from offset 8
        public string Format;        //WAVE, usually
    }
    public class fmtChunk
    {
        public string Subchunk1ID;    	//Four bytes: "fmt "
        public uint Subchunk1Size;     //Length of header
        public ushort AudioFormat;  	//1 if uncompressed
        public ushort NumChannels;       //Number of channels: 1-5
        public uint SampleRate; //In Hz
        public uint ByteRate;//For estimating RAM allocation
        public ushort BlockAlign;     //Sample frame size in bytes
        public ushort BitsPerSample; //Bits per sample
        /* More data can be contained in this chunk; specifically
         * the compression format data.  See MS website for this.
         */
    }
    public class factChunk
    {
        public string sChunkID;    		//Four bytes: "fact"
        public uint dwChunkSize;    	//Length of header
        public uint dwNumSamples;    	//Number of audio frames;
        //numsamples/samplerate should equal file length in seconds.
    }
    public class dataChunk
    {
        public string Subchunk2ID;    		//Four bytes: "data"
        public uint Subchunk2Size;    	//Length of header

        //The following non-standard fields were created to simplify
        //editing.  We need to know, for filestream seeking purposes,
        //the beginning file position of the data chunk.  It's useful to
        //hold the number of samples in the data chunk itself.  Finally,
        //the minute and second length of the file are useful to output
        //to XML.
        public long lFilePosition;	//Position of data chunk in file
        public uint dwMinLength;		//Length of audio in minutes
        public double dSecLength;		//Length of audio in seconds
        public uint NumSamples;		//Number of audio frames
        public List<Sample> Samples;
        //Different arrays for the different frame sizes
        //public byte  [] byteArray; 	//8 bit - unsigned
        //public short [] shortArray;    //16 bit - signed
    }
    public class Channel
    {
        public Int32 data;
        public Channel (Int32 d)
        {
            data = d;
        }
        public Channel() { }
    }
    public class Sample
    {
        public List<Channel> Channels;
        public Sample() { }

    }

    public class WaveFile
    {
        public riffChunk riffheader;
        public fmtChunk format;
        public factChunk fact;
        public dataChunk data;
        public string WriteHeader()
        {
            string temp="";
            temp += riffheader.FileName + "\r\n";
            temp += "ChunkID " + riffheader.ChunkID + "\r\n";
            temp += "ChunkSize " + riffheader.ChunkSize + "\r\n";
            temp += "Format " + riffheader.Format + "\r\n\r\n";
            return temp;
        }
        public string WriteFormat()
        {
            string temp = "";
            temp += "SubChunk1ID " + format.Subchunk1ID + "\r\n";
            temp += "SubChunk1Size " + format.Subchunk1Size + "\r\n";
            temp += "AudioFormat " + format.AudioFormat + "\r\n";
            temp += "NumChannels " + format.NumChannels + "\r\n";
            temp += "SampleRate " + format.SampleRate + "\r\n";
            temp += "ByteRate " + format.ByteRate + "\r\n";
            temp += "BlockAlign " + format.BlockAlign + "\r\n";
            temp += "BitsPerSample " + format.BitsPerSample + "\r\n\r\n";
            return temp;
        }
        public string WriteData()
        {
            string temp = "";
            temp += "SubChunk2ID " + data.Subchunk2ID + "\r\n";
            temp += "SubChunk2Size " + data.Subchunk2Size + "\r\n";
            temp += "DataPosition " + data.lFilePosition + "\r\n";
            temp += "NumSamples " + data.NumSamples + "\r\n";
            temp += "SamplesSize " + data.Samples.Count + "\r\n\r\n";
            return temp;
        }
    }
}
