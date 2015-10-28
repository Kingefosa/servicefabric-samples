using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SparesFiles {
   public sealed class SparseFileStream : FileStream {
      [DllImport("Kernel32", CallingConvention = CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
      private static extern Int32 GetVolumeInformation(String lpRootPathName, IntPtr lpVolumeNameBuffer,
         Int32 nVolumeNameSize, IntPtr lpVolumeSerialNumber, IntPtr lpMaximumComponentLength, out FileSystemFlags lpFileSystemFlags,
         IntPtr lpFileSystemNameBuffer, Int32 nFileSystemNameSize);
      private enum FileSystemFlags : Int32 {
         FileSupportsSparseFiles = 0x00000040
      }

      public static Boolean DoesFileSystemSupportSparseStreams(String volume) {
         FileSystemFlags fileSystemFlags;
         Boolean fOk = GetVolumeInformation(volume, IntPtr.Zero, 0, IntPtr.Zero, IntPtr.Zero, out fileSystemFlags, IntPtr.Zero, 0) != 0;
         fOk = fOk && ((fileSystemFlags & FileSystemFlags.FileSupportsSparseFiles) != 0);
         return fOk;
      }
      public static Boolean DoesFileContainAnySparseStreams(String pathname) {
         return (File.GetAttributes(pathname) & FileAttributes.SparseFile) != 0;
      }

      public SparseFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize = 0, FileOptions options = FileOptions.None)
         : base(path, mode, access, share, bufferSize, options) { }

      private struct FILETIME {
         public Int32 dwLowDateTime;
         public Int32 dwHighDateTime;
      }

      private struct ByHandleFileInformation {
         public FileAttributes dwFileAttributes;
         public FILETIME ftCreationTime;
         public FILETIME ftLastAccessTime;
         public FILETIME ftLastWriteTime;
         public Int32 dwVolumeSerialNumber;
         public Int32 nFileSizeHigh;
         public Int32 nFileSizeLow;
         public Int32 nNumberOfLinks;
         public Int32 nFileIndexHigh;
         public Int32 nFileIndexLow;
      }


      [DllImport("Kernel32")]
      private static extern Int32 GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation lpFileInformation);

      public Boolean IsStreamSparse {
         get {
            ByHandleFileInformation bhfi;
            GetFileInformationByHandle(base.SafeFileHandle, out bhfi);
            return (bhfi.dwFileAttributes & FileAttributes.SparseFile) != 0;
         }
      }

      [DllImport("Kernel32")]
      private static extern unsafe Int32 DeviceIoControl(SafeFileHandle hDevice, Int32 dwIoControlCode,
         void* pInBuffer, Int32 nInBufferSize, void* pOutBuffer, Int32 nOutBufferSize,
         out Int32 pBytesReturned, NativeOverlapped* pOverlapped);


      public void MakeSparse() {
         Int32 dw;
         const Int32 FSCTL_SET_SPARSE = 590020;
         unsafe { DeviceIoControl(base.SafeFileHandle, FSCTL_SET_SPARSE, null, 0, null, 0, out dw, null); }
      }


      private struct FileZeroDataInformation {
         public Int64 FileOffset;
         public Int64 BeyondFinalZero;
      }
      public void DecommitPortionOfStream(Int64 qwFileOffsetStart, Int64 qwFileOffsetEnd) {
         Int32 dw;
         FileZeroDataInformation fzdi = new FileZeroDataInformation {
            FileOffset = qwFileOffsetStart,
            BeyondFinalZero = qwFileOffsetEnd
         };
         const Int32 FSCTL_SET_ZERO_DATA = 622792;
         unsafe
         {
            DeviceIoControl(base.SafeFileHandle, FSCTL_SET_ZERO_DATA,
               &fzdi, sizeof(FileZeroDataInformation), null, 0, out dw, null);
         }
      }

      public struct FileAllocatedRangeBuffer {
         public Int64 FileOffset;
         public Int64 Length;
      }
      public unsafe FileAllocatedRangeBuffer[] QueryAllocatedRanges() {
         unsafe
         {
            const Int32 max = 100;  // I arbitrarily selected 100
            var farbs = stackalloc FileAllocatedRangeBuffer[max];
            FileAllocatedRangeBuffer portionOfFile = new FileAllocatedRangeBuffer { FileOffset = 0, Length = Length };

            Int32 bytesReturned;
            const Int32 FSCTL_QUERY_ALLOCATED_RANGES = 606415;
            Int32 fOk = DeviceIoControl(base.SafeFileHandle, FSCTL_QUERY_ALLOCATED_RANGES,
               &portionOfFile, sizeof(FileAllocatedRangeBuffer),
               &(farbs[0]), max * sizeof(FileAllocatedRangeBuffer), out bytesReturned, null);
            var ret = new FileAllocatedRangeBuffer[bytesReturned / sizeof(FileAllocatedRangeBuffer)];
            for (int i = 0; i < ret.Length; i++) ret[i] = farbs[i];
            return ret;
         }
      }
   }
}

#if false
   int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR, int) {
      TCHAR szPathName[] = __TEXT("D:\\SparseFile");

      if (!CSparseStream::DoesFileSystemSupportSparseStreams("D:\\")) {
         // run "ChkNtfs /e"
         MessageBox(NULL, "File system doesn't support Sparse Files", NULL,
                    MB_OK);
         return (0);
      }

      HANDLE hstream = CreateFile(szPathName, GENERIC_READ | GENERIC_WRITE,
                                  0, NULL, CREATE_ALWAYS, 0, NULL);
      CSparseStream ss(hstream);
      BOOL f = ss.MakeSparse();
      f = ss.IsStreamSparse();


      DWORD dwNumEntries, cb;
      SetFilePointer(ss, 50 * 1024 * 1024, NULL, FILE_BEGIN);
      WriteFile(ss, "A", 1, &cb, NULL);
      cb = GetFileSize(ss, NULL);
      cb = GetCompressedFileSize(szPathName, NULL);
      FILE_ALLOCATED_RANGE_BUFFER* pfarb = ss.QueryAllocatedRanges(&dwNumEntries);
      ss.FreeAllocatedRanges(pfarb);
      ss.DecommitPortionOfStream(0, 60 * 1024 * 1024);
      pfarb = ss.QueryAllocatedRanges(&dwNumEntries);
      ss.FreeAllocatedRanges(pfarb);
      cb = GetFileSize(ss, NULL);
      cb = GetCompressedFileSize(szPathName, NULL);

      SetFilePointer(ss, 0, NULL, FILE_BEGIN);
      SetEndOfFile(ss);

      // Put a bunch of entries in the end of the queue
      BYTE bEntry[32 * 1024 - 4];    // 100KB
      for (int x = 0; x < 7; x++) ss.AppendQueueEntry(bEntry, sizeof(bEntry));
      pfarb = ss.QueryAllocatedRanges(&dwNumEntries);
      ss.FreeAllocatedRanges(pfarb);

      // Read a bunch of entries from the beginning of the queue
      for (x = 0; x < 7; x++) {
         PVOID pvEntry = ss.ExtractQueueEntry(&cb);
         ss.FreeExtractedQueueEntry(pvEntry);
         cb = GetFileSize(ss, NULL);
         cb = GetCompressedFileSize(szPathName, NULL);
         pfarb = ss.QueryAllocatedRanges(&dwNumEntries);
         ss.FreeAllocatedRanges(pfarb);
      }
      CloseHandle(hstream);
      DeleteFile(szPathName);

      return (0);
   }
#endif