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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zSnap.API.Common
{
    /// <summary>
    /// <para>
    /// A skeleton implementing <see cref="IDisposable"/> where the
    /// <see cref="IDisposable.Dispose"/> method is a callback.
    /// </para>
    /// </summary>
    public sealed class DisposableSkeleton
        : IDisposable
    {
        /// <summary>
        /// <para>
        /// Creates a new skeleton with the specified disposer.
        /// </para>
        /// </summary>
        /// <param name="disposer">
        /// The delegate to be called when <see cref="IDisposable.Dispose"/>
        /// is called on this class.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="disposer"/> is null.
        /// </exception>
        public static DisposableSkeleton Create(Action disposer)
        {
            return new DisposableSkeleton(disposer);
        }

        private readonly Action _disposer;

        /// <summary>
        /// <para>
        /// Creates a new skeleton with the specified disposer.
        /// </para>
        /// </summary>
        /// <param name="disposer">
        /// The delegate to be called when <see cref="IDisposable.Dispose"/>
        /// is called on this class.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="disposer"/> is null.
        /// </exception>
        public DisposableSkeleton(Action disposer)
        {
            if (disposer == null)
            {
                throw new ArgumentNullException(
                    message:    "The specified callback cannot be null.",
                    paramName:  nameof(disposer)
                    );
            }

            _disposer = disposer;
        }

        void IDisposable.Dispose() => _disposer();
    }
}
