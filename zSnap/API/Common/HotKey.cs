/*
 * Copyright 2014-2016 (c) Liam McSherry <mcsherry.liam@gmail.com>
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at:
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 *      
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace zSnap.API.Common
{
    /// <summary>
    /// <para>
    /// Represents a "hot key," which will fire an event when the user
    /// presses a certain combination of keys.
    /// </para>
    /// </summary>
    public sealed class HotKey
        : IDisposable
    {
        /// <summary>
        /// <para>
        /// Encapsulates hot key-related interaction with the Win32 API.
        /// </para>
        /// </summary>
        private static class HotKeyDispatcher
        {
            /// <summary>
            /// <para>
            /// Represents a window class to be passed to to the
            /// <see cref="CreateWindow"/> function.
            /// </para>
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private struct WNDCLASS
            {
                public WNDCLASS(
                    uint style, WindowProcedure wndProc, int classExtra,
                    int windowExtra, IntPtr instanceHandle, IntPtr iconHandle,
                    IntPtr cursorHandle, IntPtr backgroundHandle, 
                    string menuName, string className
                    )
                {
                    this.Style              = style;
                    this.WndProc            = wndProc;
                    this.ClassExtra         = classExtra;
                    this.WindowExtra        = windowExtra;
                    this.InstanceHandle     = instanceHandle;
                    this.IconHandle         = iconHandle;
                    this.CursorHandle       = cursorHandle;
                    this.BackgroundHandle   = backgroundHandle;
                    this.MenuName           = menuName;
                    this.ClassName          = className;
                }

                /// <summary>
                /// <para>
                /// The class styles to be applied to windows of 
                /// this class.
                /// </para>
                /// </summary>
                public readonly uint             Style;
                /// <summary>
                /// <para>
                /// The procedure that will handle messages for
                /// windows of this class.
                /// </para>
                /// </summary>
                [MarshalAs(UnmanagedType.FunctionPtr)]
                public readonly WindowProcedure WndProc;
                /// <summary>
                /// <para>
                /// The number of extra bytes to be allocated after
                /// this structure.
                /// </para>
                /// </summary>
                public readonly int             ClassExtra;
                /// <summary>
                /// <para>
                /// The number of extra bytes to be allocated after
                /// the window instance.
                /// </para>
                /// </summary>
                public readonly int             WindowExtra;
                /// <summary>
                /// <para>
                /// A handle to the module that is registering the
                /// class.
                /// </para>
                /// </summary>
                public readonly IntPtr          InstanceHandle;
                /// <summary>
                /// <para>
                /// A handle to the icon used for the class, or
                /// <see cref="IntPtr.Zero"/> for the default icon.
                /// </para>
                /// </summary>
                public readonly IntPtr          IconHandle;
                /// <summary>
                /// <para>
                /// A handle to the cursor used for the class, or
                /// <see cref="IntPtr.Zero"/> for the default cursor.
                /// </para>
                /// </summary>
                public readonly IntPtr          CursorHandle;
                /// <summary>
                /// <para>
                /// A handle to the class background painter (brush),
                /// or <see cref="IntPtr.Zero"/> if the class paints
                /// its own background.
                /// </para>
                /// </summary>
                public readonly IntPtr          BackgroundHandle;
                /// <summary>
                /// <para>
                /// The resource name of the menu for the class, or
                /// null if there is no default menu.
                /// </para>
                /// </summary>
                [MarshalAs(UnmanagedType.LPTStr)]
                public readonly string          MenuName;
                /// <summary>
                /// <para>
                /// The name to register for this window class, or
                /// an atom identifying another class.
                /// </para>
                /// </summary>
                [MarshalAs(UnmanagedType.LPTStr)]
                public readonly string          ClassName;
            }

            /// <summary>
            /// <para>
            /// A delegate representing a message-handling procedure for
            /// a window.
            /// </para>
            /// </summary>
            /// <param name="handle">
            /// A handle to the window.
            /// </param>
            /// <param name="message">
            /// The message sent to the window.
            /// </param>
            /// <param name="lparam">
            /// Additional message information, depending on the
            /// value of <paramref name="message"/>.
            /// </param>
            /// <param name="wparam">
            /// Additional message information, depending on the
            /// value of <paramref name="message"/>.
            /// </param>
            /// <returns>
            /// The result of message processing, depending on the
            /// value of <paramref name="message"/>.
            /// </returns>
            private delegate IntPtr WindowProcedure(
                IntPtr handle, uint message, UIntPtr wparam, IntPtr lparam
                );

            /// <summary>
            /// <para>
            /// An additional modifier passed to the function
            /// <see cref="RegisterHotKey(IntPtr, int, uint, uint)"/> to
            /// indicate that a hot key shouldn't fire multiple times if
            /// the combination is held down.
            /// </para>
            /// </summary>
            private const uint MOD_NOREPEAT = 0x4000;
            /// <summary>
            /// <para>
            /// The identifier for the message received when a registered
            /// hot key is pressed.
            /// </para>
            /// </summary>
            private const int WM_HOTKEY = 0x0312;
            /// <summary>
            /// <para>
            /// The error code set as the last error by 
            /// <see cref="RegisterHotKey(IntPtr, int, uint, uint)"/> when
            /// the specified hot key is already registered.
            /// </para>
            /// </summary>
            private const int ERROR_HOTKEY_ALREADY_REGISTERED = 0x0581;

            /// <summary>
            /// <para>
            /// The next value to be used as a hot key identifier.
            /// </para>
            /// </summary>
            private static int _hkid;
            /// <summary>
            /// <para>
            /// The handle of the window receiving hot key notifications.
            /// </para>
            /// </summary>
            private static IntPtr _hWnd;
            /// <summary>
            /// <para>
            /// All hot key callbacks for hot keys registered through this
            /// class, keyed by the hot key identifier.
            /// </para>
            /// </summary>
            private static IDictionary<UIntPtr, Action> _hkCallbacks;
            /// <summary>
            /// <para>
            /// The struct containing information on the window class of the
            /// window created to receive hot key notifications.
            /// </para>
            /// </summary>
            private static WNDCLASS _wndClass;

            /// <summary>
            /// <para>
            /// Utility to convert <see cref="Key"/>s to keycodes.
            /// </para>
            /// </summary>
            private static KeyConverter _keyConv;


            /// <summary>
            /// <para>
            /// Handles messages for the window to which <see cref="_hWnd"/> is
            /// a handle, and calls the callbacks for hot key activations.
            /// </para>
            /// </summary>
            /// <param name="handle"></param>
            /// <param name="message"></param>
            /// <param name="wparam"></param>
            /// <param name="lparam"></param>
            /// <returns></returns>
            private static IntPtr _HandleMessages(
                IntPtr handle, uint message, UIntPtr wparam, IntPtr lparam
                )
            {
                Action callback;

                // If a hot key is activated and we have a callback for it,
                // call the callback.
                if (message == WM_HOTKEY && 
                    _hkCallbacks.TryGetValue(wparam, out callback))
                {
                    callback();
                }

                return DefWindowProc(handle, message, wparam, lparam);
            }

            /// <summary>
            /// <para>
            /// Initialises the <see cref="HotKeyDispatcher"/> class if it
            /// isn't already initialised.
            /// </para>
            /// </summary>
            private static void _Init()
            {
                // If not null, the class is already configured.
                if (_hkCallbacks != null)
                    return;

                _wndClass = new WNDCLASS
                (
                    style: 0,
                    wndProc: HotKeyDispatcher._HandleMessages,
                    classExtra: 0,
                    windowExtra: 0,
                    instanceHandle: Interop.ModuleHandle,
                    iconHandle: IntPtr.Zero, // NULL
                    cursorHandle: IntPtr.Zero, // NULL
                    backgroundHandle: IntPtr.Zero, // NULL
                    menuName: null,
                    className: typeof(WNDCLASS).AssemblyQualifiedName
                );

                // Register the class we'll use for the hot key window,
                // and receive an atom uniquely identifying that class.
                var atom = RegisterClass(ref _wndClass);

                _hWnd = CreateWindow(
                    className:  _wndClass.ClassName,
                    windowName: String.Empty,
                    style:      0,
                    x:          0,
                    y:          0,
                    width:      0,
                    height:     0,
                    parent:     IntPtr.Zero, // NULL
                    menu:       IntPtr.Zero, // NULL
                    instance:   Interop.ModuleHandle,
                    param:      IntPtr.Zero // NULL
                    );

                // We're using a dictionary so we can remove items without
                // modifying indices, which isn't possible with a list.
                _hkCallbacks = new Dictionary<UIntPtr, Action>();

                _keyConv = new KeyConverter();
            }

            [DllImport(dllName: "user32.dll",
                       CallingConvention = CallingConvention.Winapi,
                       SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool RegisterHotKey(
                IntPtr handle, int id, uint modifiers, uint vk
                );

            [DllImport(dllName: "user32.dll",
                       CallingConvention = CallingConvention.Winapi,
                       SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnregisterHotKey(
                IntPtr handle, int id
                );

            [DllImport(dllName: "user32.dll",
                       CallingConvention = CallingConvention.Winapi,
                       SetLastError = true)]
            private static extern IntPtr CreateWindow(
                [MarshalAs(UnmanagedType.LPStr)]    string  className,
                [MarshalAs(UnmanagedType.LPStr)]    string  windowName,
                                                    uint    style,
                                                    int     x,
                                                    int     y,
                                                    int     width,
                                                    int     height,
                                                    IntPtr  parent,
                                                    IntPtr  menu,
                                                    IntPtr  instance,
                                                    IntPtr  param
                );

            [DllImport(dllName: "user32.dll",
                       CallingConvention = CallingConvention.Winapi,
                       SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DestroyWindow(IntPtr hWnd);

            [DllImport(dllName: "user32.dll",
                       CallingConvention = CallingConvention.Winapi,
                       SetLastError = true)]
            private static extern IntPtr DefWindowProc(
                IntPtr hWnd, uint messaage, UIntPtr wparam, IntPtr lparam
                );

            [DllImport(dllName: "user32.dll",
                       CallingConvention = CallingConvention.Winapi,
                       SetLastError = true)]
            private static extern ushort RegisterClass(ref WNDCLASS wndClass);

            /// <summary>
            /// <para>
            /// Registers a hot key.
            /// </para>
            /// </summary>
            /// <param name="modifiers">
            /// One or more modifier keys which must be pressed in
            /// addition to <paramref name="key"/> in order for the
            /// hot key to be activated.
            /// </param>
            /// <param name="key">
            /// A key which must be pressed in addition to the keys
            /// given by <paramref name="modifiers"/> in order for
            /// the hot key to be activated.
            /// </param>
            /// <param name="callback">
            /// A method to be called each time the hot key is
            /// activated.
            /// </param>
            /// <param name="noRepeat">
            /// Whether continuously holding the hot key combination
            /// down should result in multiple calls to
            /// <paramref name="callback"/>.
            /// </param>
            /// <returns>
            /// A <see cref="HotKeyResult"/> indicating whether
            /// registration succeeded, and which provides resources
            /// for managing the hot key.
            /// </returns>
            /// <exception cref="ArgumentNullException">
            /// Thrown if <paramref name="callback"/> is null.
            /// </exception>
            public static HotKeyResult Register(
                ModifierKeys modifiers, Key key, Action callback,
                bool noRepeat = true
                )
            {
                if (callback == null)
                {
                    throw new ArgumentNullException(
                        message:    "The specified callback cannot be null.",
                        paramName:  nameof(callback)
                        );
                }

                _Init();

                // MOD_NOREPEAT is passed as a flag in the same parameter
                // as the modifier keys.
                var mods = (uint)modifiers | (noRepeat ? MOD_NOREPEAT : 0);
                // This is probably the proper way to do this, right? I'm
                // sure it's valid to just cast the Key enumeration, too.
                var keyCode = (uint)_keyConv.ConvertTo(key, typeof(uint));

                var id = _hkid;

                var success = RegisterHotKey(
                    handle:     _hWnd,
                    id:         id,
                    modifiers:  mods,
                    vk:         keyCode
                    );

                // If it doesn't succeed, a default-constructed result will
                // indicate failure.
                if (!success)
                {
                    return new HotKeyResult();
                }

                var idAsPtr = new UIntPtr((uint)id);

                // For some reason, a hot key identifier is an int in the
                // definition of [RegisterHotKey], but is passed in the
                // 'wparam' parameter of [WndProc], which is an unsigned
                // type. We're going to cast it here, rather than elsewhere.
                _hkCallbacks[idAsPtr] = callback;

                _hkid++;

                // The result provides a callback that we're using to allow
                // unregistration of the hot key.
                return new HotKeyResult(
                    new DisposableSkeleton(delegate
                    {
                        _hkCallbacks.Remove(idAsPtr);

                        UnregisterHotKey(_hWnd, id);
                    })
                );
            }
        }

        /// <summary>
        /// <para>
        /// Represents the result of attempting to register a hot key.
        /// </para>
        /// </summary>
        private struct HotKeyResult
        {
            private readonly IDisposable    _disposable;
            private readonly bool           _success;

            /// <summary>
            /// <para>
            /// Creates a new <see cref="HotKeyResult"/> that indicates
            /// success.
            /// </para>
            /// </summary>
            /// <param name="disposable">
            /// The <see cref="IDisposable"/> that will release any unmanaged
            /// resources allocated during hot key registration.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// Thrown if <paramref name="disposable"/> is null.
            /// </exception>
            public HotKeyResult(IDisposable disposable)
            {
                if (disposable == null)
                {
                    throw new ArgumentNullException(
                        message:    "The provided IDisposable cannot be null.",
                        paramName:  nameof(disposable)
                        );
                }

                _success = true;
                _disposable = disposable;
            }

            /// <summary>
            /// <para>
            /// Whether a hot key was successfully registered.
            /// </para>
            /// </summary>
            public bool Success => _success;

            /// <summary>
            /// <para>
            /// An <see cref="IDisposable"/> that releases any
            /// unmanaged resources held by the <see cref="HotKeyDispatcher"/>
            /// for the created hot key.
            /// </para>
            /// </summary>
            /// <remarks>
            /// Null if <see cref="Success"/> is false.
            /// </remarks>
            public IDisposable Disposable => _disposable;
        }

        /// <summary>
        /// <para>
        /// Creates a hot key fired by the specified key combination.
        /// </para>
        /// </summary>
        /// <param name="modifiers">
        /// The modifier keys that must be held while entering the
        /// hot key.
        /// </param>
        /// <param name="key">
        /// The key, held in combination with the modifier keys, which
        /// will fire the hot key.
        /// </param>
        /// <param name="hotKey">
        /// The variable to which the resultant <see cref="HotKey"/> instance
        /// is to be written.
        /// </param>
        /// <returns>
        /// True if the hot key was created, false if otherwise.
        /// </returns>
        public static bool TryCreate(
            ModifierKeys modifiers, 
            Key key,
            out HotKey hotKey
            )
        {
            return TryCreate(modifiers, key, false, out hotKey);
        }
        /// <summary>
        /// <para>
        /// Creates a hot key fired by the specified key combination.
        /// </para>
        /// </summary>
        /// <param name="modifiers">
        /// The modifier keys that must be held while entering the
        /// hot key.
        /// </param>
        /// <param name="key">
        /// The key, held in combination with the modifier keys, which
        /// will fire the hot key.
        /// </param>
        /// <param name="noRepeat">
        /// Whether holding down the key combination should fire the
        /// hot key multiple times.
        /// </param>
        /// <param name="hotKey">
        /// The variable to which the resultant <see cref="HotKey"/> instance
        /// is to be written.
        /// </param>
        /// <returns>
        /// True if the hot key was created, false if otherwise.
        /// </returns>
        public static bool TryCreate(
            ModifierKeys modifiers,
            Key key,
            bool noRepeat,
            out HotKey hotKey
            )
        {
            var hk = new HotKey();

            var result = HotKeyDispatcher.Register(
                modifiers:  modifiers,
                key:        key,
                callback:   hk.HandleHotKeyActivation,
                noRepeat:   noRepeat
                );

            if (!result.Success)
            {
                hotKey = null;
                return false;
            }

            hk._hkResult = result;

            hotKey = hk;
            return true;
        }

        private HotKeyResult _hkResult;

        /// <summary>
        /// <para>
        /// The callback to be passed to <see cref="HotKeyDispatcher.Register"/>
        /// to handle the activation of a hot key.
        /// </para>
        /// </summary>
        private void HandleHotKeyActivation()
        {
            if (this.IsEnabled)
            {
                this.Pressed?.Invoke(this, EventArgs.Empty);
            }
        }

        private HotKey()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <para>
        /// Fired when the key combination for the hot key is pressed.
        /// </para>
        /// </summary>
        public event EventHandler Pressed;

        /// <summary>
        /// <para>
        /// Whether the <see cref="Pressed"/> event will be fired
        /// when the key combination for it is pressed.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// When this property is set to false, the hot key remains
        /// active and cannot be assumed by another application, but
        /// the <see cref="Pressed"/> event will not fire.
        /// </para>
        /// <para>
        /// No unmanaged resources are released by setting this property
        /// to false.
        /// </para>
        /// </remarks>
        public bool IsEnabled
        {
            get; private set;
        } = true;

        /*
         * We provide Enable/Disable methods to kill two birds with one
         * stone: 1) to provide an intuitive way to allow or prohibit the
         * firing of the Pressed event; and 2) to negate the need for a
         * simple delegate to be declared if the user intends the setting
         * of IsEnabled to be an Action parameter to another
         * method.
         */

        /// <summary>
        /// <para>
        /// Enables the firing of <see cref="Pressed"/> if it is
        /// disabled.
        /// </para>
        /// </summary>
        public void Enable()
        {
            this.IsEnabled = true;
        }
        /// <summary>
        /// <para>
        /// Disables the firing of <see cref="Pressed"/> if it is
        /// enabled.
        /// </para>
        /// </summary>
        public void Disable()
        {
            this.IsEnabled = false;
        }

        /// <summary>
        /// <para>
        /// Releases the unmanaged resources held by this instance.
        /// </para>
        /// </summary>
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
