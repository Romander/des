using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DES_CBC
{
    public partial class MainWindow
    {
        private const int SizeOfBlock = 128;     
        private const int SizeOfChar = 16;       
        private const int ShiftKey = 2;          
        private const int QuantityOfRounds = 16; 
        private string[] _blocks;
        private readonly List<TextBox> _encryptKeys = new List<TextBox>();
        private readonly List<TextBox> _decryptKeys = new List<TextBox>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonEncrypt_Click(object sender, EventArgs e)
        {
            if (_encryptKeys.All(x=> x.Text.Length > 0))
            {
                EncryptDes("in.txt", "encrypt0_in.txt", 0);
                for (var i = 1; i < _encryptKeys.Count; i++)
                {
                    EncryptDes($"encrypt{i-1}_in.txt", $"encrypt{i}_in.txt", i);
                }
            }
            else
                MessageBox.Show("Введите ключевые слово!");
        }

        private void buttonDecipher_Click(object sender, EventArgs e)
        {
            if (_decryptKeys.All(x => x.Text.Length > 0))
            {
                DecryptDes($"encrypt{_encryptKeys.Count-1}_in.txt", "decrypt0_in.txt", 0);
                for (var i = 1; i < _encryptKeys.Count; i++)
                {
                    DecryptDes($"decrypt{i - 1}_in.txt", $"decrypt{i}_in.txt", i);
                }
                Process.Start($"decrypt{_encryptKeys.Count-1}_in.txt");
            }
            else
                MessageBox.Show("Введите ключевое слово!");
        }

        private void EncryptDes(string inputfile, string outputfile, int index)
        {
            var s = "";
            var key = _encryptKeys[index].Text;
            using (var sr = new StreamReader(inputfile, Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    s += sr.ReadLine();
                }
            }
            s = StringToRightLength(s);
            CutStringIntoBlocks(s);

            key = CorrectKeyWord(key, s.Length / (2 * _blocks.Length));
            key = StringToBinaryFormat(key);

            for (var j = 0; j < QuantityOfRounds; j++)
            {
                for (var i = 0; i < _blocks.Length; i++)
                    _blocks[i] = EncodeDES_One_Round(_blocks[i], key);

                key = KeyToNextRound(key);
            }

            key = KeyToPrevRound(key);

            _decryptKeys[_decryptKeys.Count-1-index].Text = StringFromBinaryToNormalFormat(key);

            var result = _blocks.Aggregate("", (current, t) => current + t);

            using (var sw = new StreamWriter(outputfile, false, Encoding.UTF8))
            {
                sw.WriteLine(StringFromBinaryToNormalFormat(result));
                sw.Close();
            }
        }
        private void DecryptDes(string inputfile, string outputfile, int index)
        {
            var s = "";

            var key = StringToBinaryFormat(_decryptKeys[index].Text);

            using (var sr = new StreamReader(inputfile, Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    s += sr.ReadLine();
                }
            }

            s = StringToBinaryFormat(s);

            CutBinaryStringIntoBlocks(s);

            for (var j = 0; j < QuantityOfRounds; j++)
            {
                for (var i = 0; i < _blocks.Length; i++)
                    _blocks[i] = DecodeDES_One_Round(_blocks[i], key);

                key = KeyToPrevRound(key);
            }

            var result = _blocks.Aggregate("", (current, t) => current + t);

            using (var sw = new StreamWriter(outputfile, false, Encoding.UTF8))
            {
                sw.WriteLine(StringFromBinaryToNormalFormat(result));
            }
        }

        private string StringToRightLength(string input)
        {
            while (((input.Length * SizeOfChar) % SizeOfBlock) != 0)
                input += "#";

            return input;
        }

        private void CutStringIntoBlocks(string input)
        {
            _blocks = new string[(input.Length * SizeOfChar) / SizeOfBlock];

            var lengthOfBlock = input.Length / _blocks.Length;

            for (var i = 0; i < _blocks.Length; i++)
            {
                _blocks[i] = input.Substring(i * lengthOfBlock, lengthOfBlock);
                _blocks[i] = StringToBinaryFormat(_blocks[i]);
            }
        }

        private void CutBinaryStringIntoBlocks(string input)
        {
            _blocks = new string[input.Length / SizeOfBlock];

            var lengthOfBlock = input.Length / _blocks.Length;

            for (var i = 0; i < _blocks.Length; i++)
                _blocks[i] = input.Substring(i * lengthOfBlock, lengthOfBlock);
        }

        private static string StringToBinaryFormat(string input)
        {
            var output = "";
            foreach (var t in input)
            {
                var charBinary = Convert.ToString(t, 2);
                while (charBinary.Length < SizeOfChar)
                    charBinary = "0" + charBinary;
                output += charBinary;
            }

            return output;
        }

        private static string CorrectKeyWord(string input, int lengthKey)
        {
            if (input.Length > lengthKey)
                input = input.Substring(0, lengthKey);
            else
                while (input.Length < lengthKey)
                    input = "0" + input;

            return input;
        }

        private static string EncodeDES_One_Round(string input, string key)
        {
            var l = input.Substring(0, input.Length / 2);
            var r = input.Substring(input.Length / 2, input.Length / 2);

            return (r + Xor(l, F(r, key)));
        }

        private static string DecodeDES_One_Round(string input, string key)
        {
            var l = input.Substring(0, input.Length / 2);
            var r = input.Substring(input.Length / 2, input.Length / 2);
            return (Xor(F(l, key), r) + l);
        }

        private static string Xor(string s1, string s2)
        {
            var result = "";

            for (var i = 0; i < s1.Length; i++)
            {
                var a = Convert.ToBoolean(Convert.ToInt32(s1[i].ToString()));
                var b = Convert.ToBoolean(Convert.ToInt32(s2[i].ToString()));

                if (a ^ b)
                    result += "1";
                else
                    result += "0";
            }
            return result;
        }

        private static string F(string s1, string s2)
        {
            return Xor(s1, s2);
        }

        private static string KeyToNextRound(string key)
        {
            for (var i = 0; i < ShiftKey; i++)
            {
                key = key[key.Length - 1] + key;
                key = key.Remove(key.Length - 1);
            }
            return key;
        }

        private static string KeyToPrevRound(string key)
        {
            for (var i = 0; i < ShiftKey; i++)
            {
                key = key + key[0];
                key = key.Remove(0, 1);
            }
            return key;
        }

        private static string StringFromBinaryToNormalFormat(string input)
        {
            var output = "";
            while (input.Length > 0)
            {
                var charBinary = input.Substring(0, SizeOfChar);
                input = input.Remove(0, SizeOfChar);
                var degree = charBinary.Length - 1;
                var a = charBinary.Sum(c => Convert.ToInt32(c.ToString())*(int) Math.Pow(2, degree--));
                output += ((char)a).ToString();
            }
            return output;
        }

        private void MinusBtn_Click(object sender, RoutedEventArgs e)
        {
            var i = int.Parse(CountBox.Text);
            if (i == 0)
                return;
            i--;
            CountBox.Text = i.ToString();
            RemoveBoxFrom(_encryptKeys.Last(), EncryptKeys, _encryptKeys);
            RemoveBoxFrom(_decryptKeys.Last(), DecryptKeys, _decryptKeys);
        }

        private void PlusBtn_Click(object sender, RoutedEventArgs e)
        {
            var i = int.Parse(CountBox.Text);
            i++;
            CountBox.Text = i.ToString();
            AddBoxTo(new TextBox(), EncryptKeys, _encryptKeys);
            AddBoxTo(new TextBox(), DecryptKeys, _decryptKeys);
        }

        private void AddBoxTo(TextBox box, StackPanel element, IList<TextBox> list)
        {
            element.Children.Add(box);
            list.Add(box);
        }
        private static void RemoveBoxFrom(TextBox box, StackPanel element, IList<TextBox> list)
        {
            element.Children.Remove(box);
            list.Remove(box);
        }

    }
}
