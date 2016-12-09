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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zSnap.API.Common
{
    /// <summary>
    /// <para>
    /// Provides methods and properties related to interoperating with
    /// native code.
    /// </para>
    /// </summary>
    public static class Interop
    {
        /// <summary>
        /// <para>
        /// Retrieves the handle of the specified module, or of the calling
        /// module if no other module is named.
        /// </para>
        /// </summary>
        /// <param name="moduleName">
        /// The name of the module the handle of which is to be retrieved.
        /// </param>
        /// <returns>
        /// A handle to the specified module.
        /// </returns>
        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi,
                         SetLastError = true)]
        private static extern IntPtr GetModuleHandle(
            [MarshalAs(UnmanagedType.LPStr)] string moduleName = null
            );


        private static Lazy<IntPtr> _hModule = new Lazy<IntPtr>(delegate
        {
            var hModule = GetModuleHandle();

            if (hModule == IntPtr.Zero) // NULL
            {
                throw new InvalidProgramException(
                    "Call to GetModuleHandle failed, " +
                    $"error {Marshal.GetLastWin32Error():N}."
                    );
            }

            return hModule;
        });

        /// <summary>
        /// <para>
        /// The handle to the zSnap module.
        /// </para>
        /// </summary>
        public static IntPtr ModuleHandle => _hModule.Value;
    }
}
