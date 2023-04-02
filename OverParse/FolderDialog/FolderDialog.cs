using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Interop;

namespace FolderDialog
{
    /// <summary>
    /// Prompts the user to select a folder.
    /// </summary>
    /// <remarks>
    /// This class will use the Vista style Select Folder dialog if possible, or the regular FolderBrowserDialog
    /// if it is not. Note that the Vista style dialog is very different, so using this class without testing
    /// in both Vista and older Windows versions is not recommended.
    /// </remarks>
    /// <threadsafety instance="false" static="true" />
    public sealed class FolderBrowserDialog
    {
        private string _description;
        private string _selectedPath;

        /// <summary>
        /// Creates a new instance of the <see cref="FolderBrowserDialog" /> class.
        /// </summary>
        public FolderBrowserDialog() => Reset();

        #region Public Properties


        /// <summary>
        /// Gets or sets the descriptive text displayed above the tree view control in the dialog box, or below the list view control
        /// in the Vista style dialog.
        /// </summary>
        /// <value>
        /// The description to display. The default is an empty string ("").
        /// </value>
        public string Description
        {
            get => _description ?? string.Empty;
            set => _description = value;
        }

        /// <summary>
        /// Gets or sets the root folder where the browsing starts from. This property has no effect if the Vista style
        /// dialog is used.
        /// </summary>
        /// <value>
        /// One of the <see cref="Environment.SpecialFolder" /> values. The default is Desktop.
        /// </value>
        /// <exception cref="InvalidEnumArgumentException">The value assigned is not one of the <see cref="System.Environment.SpecialFolder" /> values.</exception>
        public Environment.SpecialFolder RootFolder { get; set; }

        /// <summary>
        /// Gets or sets the path selected by the user.
        /// </summary>
        /// <value>
        /// The path of the folder first selected in the dialog box or the last folder selected by the user. The default is an empty string ("").
        /// </value>
        public string SelectedPath
        {
            get => _selectedPath ?? string.Empty;
            set => _selectedPath = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the New Folder button appears in the folder browser dialog box. This
        /// property has no effect if the Vista style dialog is used; in that case, the New Folder button is always shown.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the New Folder button is shown in the dialog box; otherwise, <see langword="false" />. The default is <see langword="true" />.
        /// </value>
        public bool ShowNewFolderButton { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether to use the value of the <see cref="Description" /> property
        /// as the dialog title for Vista style dialogs. This property has no effect on old style dialogs.
        /// </summary>
        /// <value><see langword="true" /> to indicate that the value of the <see cref="Description" /> property is used as dialog title; <see langword="false" />
        /// to indicate the value is added as additional text to the dialog. The default is <see langword="false" />.</value>
        public bool UseDescriptionForTitle { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets all properties to their default values.
        /// </summary>
        public void Reset()
        {
            _description = string.Empty;
            UseDescriptionForTitle = false;
            _selectedPath = string.Empty;
            RootFolder = Environment.SpecialFolder.Desktop;
            ShowNewFolderButton = true;
        }

        /// <summary>
        /// Displays the folder browser dialog.
        /// </summary>
        /// <returns>If the user clicks the OK button, <see langword="true" /> is returned; otherwise, <see langword="false" />.</returns>
        public bool? ShowDialog() => ShowDialog(null);

        /// <summary>
        /// Displays the folder browser dialog.
        /// </summary>
        /// <param name="owner">Handle to the window that owns the dialog.</param>
        /// <returns>If the user clicks the OK button, <see langword="true" /> is returned; otherwise, <see langword="false" />.</returns>
        public bool? ShowDialog(Window owner)
        {
            IntPtr ownerHandle = owner == null ? NativeMethods.GetActiveWindow() : new WindowInteropHelper(owner).Handle;
            return new bool?(RunDialog(ownerHandle));
        }

        #endregion

        #region Private Methods

        private bool RunDialog(IntPtr owner)
        {
            IFileDialog dialog = null;
            try
            {
                dialog = new INativeFileOpenDialog();
                SetDialogProperties(dialog);
                int result = dialog.Show(owner);
                if (result < 0)
                {
                    if ((uint)result == (uint)HRESULT.ERROR_CANCELLED)
                    {
                        return false;
                    }
                    else
                    {
                        throw Marshal.GetExceptionForHR(result);
                    }
                }
                GetResult(dialog);
                return true;
            }
            finally
            {
                if (dialog != null)
                {
                    Marshal.FinalReleaseComObject(dialog);
                }
            }
        }


        private void SetDialogProperties(IFileDialog dialog)
        {
            // Description
            if (!string.IsNullOrEmpty(_description))
            {
                if (UseDescriptionForTitle)
                {
                    dialog.SetTitle(_description);
                }
                else
                {
                    IFileDialogCustomize customize = (IFileDialogCustomize)dialog;
                    customize.AddText(0, _description);
                }
            }

            dialog.SetOptions(NativeMethods.FOS.PICKFOLDERS | NativeMethods.FOS.FORCEFILESYSTEM | NativeMethods.FOS.FILEMUSTEXIST);

            if (!string.IsNullOrEmpty(_selectedPath))
            {
                string parent = Path.GetDirectoryName(_selectedPath);
                if (parent == null || !Directory.Exists(parent))
                {
                    dialog.SetFileName(_selectedPath);
                }
                else
                {
                    string folder = Path.GetFileName(_selectedPath);
                    dialog.SetFolder(NativeMethods.CreateItemFromParsingName(parent));
                    dialog.SetFileName(folder);
                }
            }
        }

        private void GetResult(IFileDialog dialog)
        {
            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(NativeMethods.SIGDN.FILESYSPATH, out _selectedPath);
        }

        #endregion
    }

    internal enum HRESULT : long
    {
        S_FALSE = 0x0001,
        S_OK = 0x0000,
        E_INVALIDARG = 0x80070057,
        E_OUTOFMEMORY = 0x8007000E,
        ERROR_CANCELLED = 0x800704C7
    }

    internal static class IIDGuid
    {
        internal const string IModalWindow = "b4db1657-70d7-485e-8e3e-6fcb5a5c1802";
        internal const string IFileDialog = "42f85136-db7e-439c-85f1-e4075d135fc8";
        internal const string IFileOpenDialog = "d57c7288-d4ad-4768-be02-9d969532d960";
        internal const string IFileDialogEvents = "973510DB-7D7F-452B-8975-74A85828D354";
        internal const string IFileDialogCustomize = "e6fdd21a-163f-4975-9c8c-a69f1ba37034";
        internal const string IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
        internal const string IShellItemArray = "B63EA76D-1F85-456F-A19C-48159EFA858B";
    }

    internal static class CLSIDGuid
    {
        internal const string FileOpenDialog = "DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7";
    }

    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    class SafeModuleHandle : SafeHandle
    {
        public SafeModuleHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle() => NativeMethods.FreeLibrary(handle);
    }

    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
    class ActivationContextSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public ActivationContextSafeHandle()
            : base(true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            NativeMethods.ReleaseActCtx(handle);
            return true;
        }
    }


    #region ShellWrapperDefinitions.cs

    // Dummy base interface for CommonFileDialog coclasses


    // ---------------------------------------------------------
    // Coclass interfaces - designed to "look like" the object 
    // in the API, so that the 'new' operator can be used in a 
    // straightforward way. Behind the scenes, the C# compiler
    // morphs all 'new CoClass()' calls to 'new CoClassWrapper()'
    [ComImport,
    Guid(IIDGuid.IFileOpenDialog),
    CoClass(typeof(FileOpenDialogRCW))]
    internal interface INativeFileOpenDialog : IFileOpenDialog
    {
    }


    // ---------------------------------------------------
    // .NET classes representing runtime callable wrappers
    [ComImport,
    ClassInterface(ClassInterfaceType.None),
    TypeLibType(TypeLibTypeFlags.FCanCreate),
    Guid(CLSIDGuid.FileOpenDialog)]
    internal class FileOpenDialogRCW
    {
    }


    // TODO: make these available (we'll need them when passing in 
    // shell items to the CFD API
    //[ComImport,
    //Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"),
    //CoClass(typeof(ShellItemClass))]
    //internal interface ShellItem : IShellItem
    //{
    //}

    //// NOTE: This GUID is for CLSID_ShellItem, which
    //// actually implements IShellItem2, which has lots of 
    //// stuff we don't need
    //[ComImport,
    //ClassInterface(ClassInterfaceType.None),
    //TypeLibType(TypeLibTypeFlags.FCanCreate)]
    //internal class ShellItemClass
    //{
    //}

    #endregion ShellWrapperDefinitions.cs


    #region ShellComInterfaces.cs
    [ComImport(),
    Guid(IIDGuid.IModalWindow),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IModalWindow
    {
    }

    [ComImport(),
    Guid(IIDGuid.IFileDialog),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialog : IModalWindow
    {
        // Defined on IModalWindow - repeated here due to requirements of COM interop layer
        // --------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime),
        PreserveSig]
        int Show([In] IntPtr parent);

        // IFileDialog-Specific interface members
        // --------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypes([In] uint cFileTypes, [In, MarshalAs(UnmanagedType.LPArray)] NativeMethods.COMDLG_FILTERSPEC[] rgFilterSpec);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileTypeIndex([In] uint iFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileTypeIndex(out uint piFileType);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void Unadvise([In] uint dwCookie);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOptions([In] NativeMethods.FOS fos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetOptions(out NativeMethods.FOS pfos);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
    }

    [ComImport(),
    Guid(IIDGuid.IFileOpenDialog),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileOpenDialog : IFileDialog
    {
        // Defined on IModalWindow - repeated here due to requirements of COM interop layer
        // --------------------------------------------------------------------------------

        // Defined on IFileDialog - repeated here due to requirements of COM interop layer
    }

    [ComImport,
    Guid(IIDGuid.IFileDialogEvents),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialogEvents
    {

    }

    [ComImport,
    Guid(IIDGuid.IShellItem),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItem
    {
        // Not supported: IBindCtx
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetDisplayName([In] NativeMethods.SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
    }


    [ComImport,
    Guid(IIDGuid.IFileDialogCustomize),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFileDialogCustomize
    {
        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void AddText([In] int dwIDCtl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
    }

    #endregion ShellComInterfaces.cs

}
