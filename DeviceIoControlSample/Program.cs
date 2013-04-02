using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DeviceIoControlSample
{
	static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main()
		{
			//入力処理
			Console.WriteLine( "CDドライブ文字を入力してください（Fとか。）" );
			string volume = string.Format( @"\\.\{0}:", Console.ReadLine() );

			Console.WriteLine( "読み込み速度 kilobytes per second. を入力してください。" );
			string readSpeedString = Console.ReadLine();

			//読み取り速度の判定
			ushort readSpeed;
			if( ! ushort.TryParse( readSpeedString, out readSpeed ) )
			{
				Console.WriteLine( "入力値誤り" );
				return;
			}

			//ハンドル作成
			IntPtr handle;
			bool handleCreated = CreateHandle( volume, out  handle );
			if( !handleCreated )
			{
				Console.WriteLine( "ハンドル作成失敗" );
				return;
			}

			//CD読み込み速度指定
			bool success = SetSpeed( handle, readSpeed );
			if( success )
			{
				System.Console.WriteLine( "CD Drive is spinning speed changed\n" );
			} else {
				System.Console.WriteLine( 
					string.Format("LastWin32Error: {0}", System.Runtime.InteropServices.Marshal.GetLastWin32Error())
				);
			}

		}

		/// ハンドル作成
		/// </summary>
		/// <param name="FileName"></param>
		/// <param name="hHandle"></param>
		/// <returns></returns>
		public static bool CreateHandle( string fileName, out IntPtr handle )
		{
			// open the existing file for reading       
			handle = CreateFile
			(
				fileName,
				GENERIC_READ,
				FILE_SHARE_READ,
				0,
				OPEN_EXISTING,
				0,
				0
			);

			// 物理ドライブ処理のエラーは、0xffffffffとなる
			if( handle != System.IntPtr.Zero
             && (uint)handle.ToInt32() != (uint)0xffffffff )
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// 速度指定
		/// </summary>
		/// <param name="s"></param>
		private unsafe static bool SetSpeed( IntPtr handle, ushort readSpeed )
		{
			//マネージに速度指定
			_CDROM_SET_SPEED css = new _CDROM_SET_SPEED();
			css.RequestType = _CDROM_SPEED_REQUEST.CdromSetSpeed;
			css.ReadSpeed = readSpeed;

			uint nBytes = sizeof( long );
			bool bResult = DeviceIoControl(
				handle   // デバイス、ファイル、ディレクトリいずれかのハンドル
				, IOCTL_CDROM_SET_SPEED // 実行する動作の制御コード
				, &css // 入力データを供給するバッファへのポインタ
				, (uint)sizeof( _CDROM_SET_SPEED ) // 入力バッファのバイト単位のサイズ
				, null // 出力データを受け取るバッファへのポインタ
				, 0 // 出力バッファのバイト単位のサイズ
				, &nBytes // バイト数を受け取る変数へのポインタ
				, 0  // 非同期動作を表す構造体へのポインタ
				);
			return bResult;
		}

		#region  "アンマネージ"

		/// <summary>読み取りアクセス</summary>
		const uint GENERIC_READ = 0x80000000;
		/// <summary>後続のオープン操作で読み取りアクセスが要求された場合そのオープンを許可する</summary>
		const uint FILE_SHARE_READ = 0x00000001;
		/// <summary>後続のオープン操作で書き込みアクセスが要求された場合そのオープンを許可する</summary>
		const uint FILE_SHARE_WRITE = 0x00000002;
		/// <summary>物理ディスクを指定する場合、OPEN_EXISTINGを指定しなければいけないらしい </summary>
		const uint OPEN_EXISTING = 3;
		/// <summary>メソッドの最後にMarshal.GetLastWin32Error()を呼び出してセットした値</summary>
		static int LastWin32Error;

		/// <summary>DeviceIoControlの制御コード
		/// CDドライブの回転速度指定</summary>
		/// <see cref="http://www.ioctls.net/"/>
		const uint IOCTL_CDROM_SET_SPEED = 0x24060;

		/// <summary>
		/// ハンドルの作成
		/// </summary>
		/// <see cref="http://msdn.microsoft.com/ja-jp/library/cc429198.aspx"/>
		[System.Runtime.InteropServices.DllImport( "kernel32", SetLastError = true )]
		static extern unsafe System.IntPtr CreateFile
		(
			string FileName,          // file name
			uint DesiredAccess,       // access mode
			uint ShareMode,           // share mode
			uint SecurityAttributes,  // Security Attributes
			uint CreationDisposition, // how to create
			uint FlagsAndAttributes,  // file attributes
			int hTemplateFile         // handle to template file
		);

		/// <summary>
		/// デバイスに対する操作関数（DeviceIoControl）
		/// </summary>
		/// <see cref="http://msdn.microsoft.com/ja-jp/library/cc429164.aspx"/>
		[System.Runtime.InteropServices.DllImport( "kernel32", SetLastError = true )]
		static extern unsafe bool DeviceIoControl(
			System.IntPtr hFile,      // handle to file
			uint dwIoControlCode,     // 実行する動作の制御コード
			void* lpInBuffer,         // 入力データを供給するバッファへのポインタ
			uint nInBufferSize,       // 入力バッファのバイト単位のサイズ
			void* lpOutBuffer,        // 出力データを受け取るバッファへのポインタ
			uint nOutBufferSize,      // 出力バッファのバイト単位のサイズ
			uint* lpBytesReturned,    // バイト数を受け取る変数へのポインタ
			int Overlapped            // overlapped buffer
		);


		/// <summary>
		/// CD操作に必要な構造体メイン
		/// The CDROM_SET_SPEED structure is used with the IOCTL_CDROM_SET_SPEED request to set the spindle speed of a CD-ROM drive during data transfers in which no data loss is permitted.
		/// </summary>
		/// <see cref="http://msdn.microsoft.com/ja-JP/library/windows/hardware/ff551368(v=vs.85).aspx"/>
		public struct _CDROM_SET_SPEED
		{
			public _CDROM_SPEED_REQUEST RequestType;
			public ushort ReadSpeed;
			public ushort WriteSpeed;
			public _WRITE_ROTATION RotationControl;
		}

		/// <summary>
		/// CD操作に必要な構造体に必要な構造体を定義
		///The CDROM_SPEED_REQUEST enumeration indicates which command that the CD-ROM class driver will use to set the spindle speed of a CD-ROM drive.
		/// </summary>
		/// <see cref="http://msdn.microsoft.com/ja-JP/library/windows/hardware/ff551370(v=vs.85).aspx"/>
		public enum _CDROM_SPEED_REQUEST
		{
			CdromSetSpeed      = 0,
			CdromSetStreaming  = 1
		}

		/// <summary>
		/// CD操作に必要な構造体に必要な構造体を定義
		/// The WRITE_ROTATION enumeration specifies whether a CD-ROM drive uses constant linear velocity (CLV) rotation or constant angular velocity (CAV) rotation when it writes to a CD.
		/// </summary>
		/// <see cref="http://msdn.microsoft.com/ja-JP/library/windows/hardware/ff568045(v=vs.85).aspx"/>
		public enum _WRITE_ROTATION
		{
			CdromDefaultRotation  = 0,
			CdromCAVRotation      = 1
		}

		#endregion  "アンマネージ"

	}
}
