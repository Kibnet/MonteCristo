using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security;
using System.Windows;

namespace IndexSorter
{
	/// <summary>
	/// Логика взаимодействия для IndexSorterWindow.xaml
	/// </summary>
	public partial class IndexSorterWindow : Window
	{
		public IndexSorterWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Преобразование переменной типа Int32 в массив байт, который копируется в массив по ссылке с заданным смещением
		/// </summary>
		/// <param name="value">Значение для преобразования</param>
		/// <param name="numArray">Ссылка на целевой массив</param>
		/// <param name="position">Позиция в целевом массиве с которой будет произведена запись</param>
		[SecuritySafeCritical]
		public static unsafe void GetBytes(int value, ref byte[] numArray, int position = 0)
		{
			fixed (byte* numPtr = numArray)
				*(int*)(numPtr + position) = value;
		}

		[SecuritySafeCritical]
		public static unsafe void GetBytes(long value, ref byte[] numArray)
		{
			fixed (byte* numPtr = numArray)
				*(long*)numPtr = value;
		}

		static DirectoryInfo dirin;
		static DirectoryInfo dirout;
		static short icnt;
		static long maxfilesize;

		private void Sort_Click(object sender, RoutedEventArgs e)
		{
			dirin = new DirectoryInfo(InPath.Text);
			if (!dirin.Exists)
			{
				dirin.Create();
			}
			dirout = new DirectoryInfo(OutPath.Text);
			if (!dirout.Exists)
			{
				dirout.Create();
			}
			var wor = new BackgroundWorker() { WorkerReportsProgress = true };
			wor.ProgressChanged += (o, args) =>
			{
				Status.Content = args.UserState.ToString();
			};
			wor.DoWork += WorOnDoWork;

			icnt = (short)FileName.Value.GetValueOrDefault();
			maxfilesize = MaxFileSize.Value.GetValueOrDefault();
			wor.RunWorkerAsync();
		}

		private void WorOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
		{
			long pcnt = 0; //Счётчик пар вершин
			long dcnt = 0; //Счётчик индексов
			var buf = new byte[12]; //Основной буффер для работы
			var list = new List<long>((int)maxfilesize); //Список для сортировки
			var datas = new MemoryStream((int)maxfilesize * 4);
			var index = new MemoryStream((int)maxfilesize * 4);
			var start = DateTime.Now;

			foreach (FileInfo file in dirin.EnumerateFiles())
			{
				using (var stream = file.OpenRead())
				{
					try
					{
						while (true)
						{
							if (8 != stream.Read(buf, 0, 8)) //Считываем пару вершин
							{
								break;
							}
							Array.Copy(buf, 0, buf, 8, 4); //Копируем первую вершину после второй
							list.Add(BitConverter.ToInt64(buf, 0)); //Добавляем пару для сортировки
							list.Add(BitConverter.ToInt64(buf, 4)); //Добавляем развёрнутую пару для сортировки
							dcnt += 2;
							++pcnt;
							if (pcnt % 100000 == 0)
							{
								((BackgroundWorker)sender).ReportProgress(0, file.Name + " загружено " + pcnt.ToString("N0") + " пар");
							}

							if (dcnt < maxfilesize) continue; //При достижении максимума записей происходит сохранение на диск
							
							((BackgroundWorker)sender).ReportProgress(0, file.Name + " сортировка " + dcnt.ToString("N0") + " пар");
							dcnt = 0;

							FlushList(sender, list, index, datas);
							// Сбрасываем данные не освобождая памяти
							index.Position = 0; 
							index.SetLength(0);
							datas.Position = 0;
							datas.SetLength(0);
						}
					}
					catch
					{
					}
				}
			}
			FlushList(sender, list, index, datas); // Докидываем остатки на диск
			datas.Close();
			index.Close();

			((BackgroundWorker)sender).ReportProgress(0, pcnt.ToString("N0") + " индексов за " + (DateTime.Now - start).TotalSeconds.ToString("F2") + " секунд");
		}

		private static void FlushList(object sender, List<long> list, MemoryStream index, MemoryStream datas)
		{
			list.Sort(); //Сортируем список
			byte[] buf = new byte[8];
			short len = 0;
			var last = int.MinValue;
			long dcnt = 0;
			foreach (long l in list)
			{
				GetBytes(l, ref buf); //Получаем массив байтов
				datas.Write(buf, 0, 4); //Записываем данные
				var key = BitConverter.ToInt32(buf, 4);
				if (last != key)
				{
					if (len != 0)
					{
						GetBytes(last, ref buf);
						GetBytes(len, ref buf, 4);
						index.Write(buf, 0, 6); //Записываем индекс
					}
					last = key;
					len = 1;
				}
				else
				{
					len++;
				}
				++dcnt;
				if (dcnt%100000 == 0)
				{
					((BackgroundWorker) sender).ReportProgress(0, "Cохранено " + dcnt.ToString("N0") + " пар");
				}
			}
			
			list.Clear();

			if (len != 0) //Дозаписываем остатки индекса
			{
				GetBytes(last, ref buf);
				GetBytes(len, ref buf, 4);
				index.Write(buf, 0, 6); //Записываем индекс
			}

			if (datas.Length > 0)
			{
				using (var datstr = File.Create(dirout.FullName + "\\datas-" + icnt.ToString("D5")))
				{
					datas.Position = 0;
					datas.CopyTo(datstr);
				}
			}
			if (index.Length > 0)
			{
				using (var indstr = File.Create(dirout.FullName + "\\index-" + icnt.ToString("D5")))
				{
					index.Position = 0;
					index.CopyTo(indstr);
				}
			}

			icnt++;
		}
	}
}
