﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MsgWriter.Enums;
using MsgWriter.Exceptions;
using MsgWriter.Structures;
using OpenMcdf;
// ReSharper disable InconsistentNaming

/*
   Copyright 2015 - 2016 Kees van Spelde

   Licensed under The Code Project Open License (CPOL) 1.02;
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.codeproject.com/info/cpol10.aspx

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

namespace MsgWriter
{
    /// <summary>
    ///     The base class for all the different types of Outlook MSG files
    /// </summary>
    public class Message : IDisposable
    {
        #region Fields
        /// <summary>
        ///     The <see cref="OpenMcdf.CompoundFile" />
        /// </summary>
        internal readonly CompoundFile CompoundFile;

        /// <summary>
        ///     The message class
        /// </summary>
        internal MessageClass Class;

        /// <summary>
        ///     The "__nameid_version1.0" storage
        /// </summary>
        private readonly CFStorage _nameIdStorage;
        #endregion

        #region Constructor
        /// <summary>
        ///     Creates this object and sets all it's properties
        /// </summary>
        internal Message()
        {
            CompoundFile = new CompoundFile();

            // In the preceding figure, the "__nameid_version1.0" named property mapping storage contains the 
            // three streams  used to provide a mapping from property ID to property name 
            // ("__substg1.0_00020102", "__substg1.0_00030102", and "__substg1.0_00040102") and various other 
            // streams that provide a mapping from property names to property IDs.
            _nameIdStorage = CompoundFile.RootStorage.TryGetStorage(PropertyTags.NameIdStorage) ??
                             CompoundFile.RootStorage.AddStorage(PropertyTags.NameIdStorage);

            var stream1 = _nameIdStorage.AddStream(PropertyTags.EntryStream);
            stream1.SetData(new byte[0]);
            var stream2 = _nameIdStorage.AddStream(PropertyTags.NameIdStream2);
            stream2.SetData(new byte[0]);
            var stream3 = _nameIdStorage.AddStream(PropertyTags.NameIdStream3);
            stream3.SetData(new byte[0]);
        }
        #endregion

        #region Save
        /// <summary>
        ///     Saves the message to the given <paramref name="fileName" />
        /// </summary>
        /// <param name="fileName"></param>
        internal void Save(string fileName)
        {
            CompoundFile.Save(fileName);
        }

        /// <summary>
        ///     Saves the message to the given <paramref name="stream" />
        /// </summary>
        /// <param name="stream"></param>
        internal void Save(Stream stream)
        {
            CompoundFile.Save(stream);
        }
        #endregion

        #region GetString
        /// <summary>
        ///     Returns the string value for the given <param name="propertyTag">
        ///         .
        ///         <c>null</c> is returned when the property does not exists or no valid value is found
        ///     </param>
        /// </summary>
        /// <param name="propertyTag">
        ///     <see cref="PropertyTag" />
        /// </param>
        /// <returns></returns>
        /// <exception cref="MWInvalidProperty">
        ///     Raised when the <paramref name="propertyTag" /> is not of the type
        ///     <see cref="PropertyType.PT_STRING8" /> or <see cref="PropertyType.PT_UNICODE" />
        /// </exception>
        internal string GetString(PropertyTag propertyTag)
        {
            return GetString(new List<PropertyTag> {propertyTag});
        }

        /// <summary>
        ///     Returns the string value from the first item in the list of
        ///     <param name="propertyTags">
        ///         that gives
        ///         back a valid value. <c>null</c> is returned when the property does not exists or no valid value is found
        ///     </param>
        /// </summary>
        /// <param name="propertyTags">List of <see cref="PropertyTag" /></param>
        /// <returns></returns>
        /// <exception cref="MWInvalidProperty">
        ///     Raised when the <paramref name="propertyTags" /> is not of the type
        ///     <see cref="PropertyType.PT_STRING8" /> or <see cref="PropertyType.PT_UNICODE" />
        /// </exception>
        internal string GetString(List<PropertyTag> propertyTags)
        {
            string result = null;

            foreach (var propertyTag in propertyTags)
            {
                var stream = CompoundFile.RootStorage.TryGetStream(propertyTag.Name);
                if (stream != null)
                {
                    switch (propertyTag.Type)
                    {
                        case PropertyType.PT_STRING8:
                            result = Encoding.Default.GetString(stream.GetData());
                            break;

                        case PropertyType.PT_UNICODE:
                            result =
                                Encoding.UTF8.GetString(CompoundFile.RootStorage.GetStream(propertyTag.Name).GetData());
                            break;

                        default:
                            throw new MWInvalidProperty("The property is not of the type PT_STRING8 or PT_UNICODE");
                    }
                }

                if (!string.IsNullOrEmpty(result))
                    return result;
            }

            return null;
        }
        #endregion

        #region AddString
        /// <summary>
        ///     Adds the given single value
        ///     <param name="propertyTag" />
        ///     to the message, any already existing <see cref="PropertyTag" /> is
        ///     overwritten.
        /// </summary>
        /// <param name="propertyTag">
        ///     <see cref="PropertyTag" />
        /// </param>
        /// <param name="value">The value</param>
        /// <returns></returns>
        /// <exception cref="MWInvalidProperty">
        ///     Raised when the <paramref name="propertyTag" /> is not of the type
        ///     <see cref="PropertyType.PT_STRING8" /> or <see cref="PropertyType.PT_UNICODE" />
        /// </exception>
        internal void AddString(PropertyTag propertyTag, string value)
        {
            var stream = CompoundFile.RootStorage.TryGetStream(propertyTag.Name);
            if (stream != null)
            {
                switch (propertyTag.Type)
                {
                    case PropertyType.PT_STRING8:
                    case PropertyType.PT_UNICODE:
                        stream.SetData(Encoding.UTF8.GetBytes(value));
                        break;

                    default:
                        throw new MWInvalidProperty("The property is not of the type PT_STRING8 or PT_UNICODE");
                }
            }
            else
            {
                stream = CompoundFile.RootStorage.AddStream(propertyTag.Name);
                stream.SetData(Encoding.Unicode.GetBytes(value));
            }
        }

        /// <summary>
        ///     Adds the given multivalue
        ///     <param name="propertyTag" />
        ///     to the message, any already existing <see cref="PropertyTag" /> is
        ///     overwritten.
        /// </summary>
        /// <param name="propertyTag">List of <see cref="PropertyTag" /></param>
        /// <param name="values">The values</param>
        /// <returns></returns>
        /// <exception cref="MWInvalidProperty">
        ///     Raised when the <paramref name="propertyTag" /> is not of the type
        ///     <see cref="PropertyType.PT_MV_STRING8" /> or <see cref="PropertyType.PT_MV_UNICODE" />
        /// </exception>
        internal void AddStrings(PropertyTag propertyTag, List<string> values)
        {
            //  “__substg1.0_<tag>-00000000”, “__substg1.0_<tag>-00000001”, etc…
            var index = 0;

            var name = propertyTag.Name + "-" + index.ToString().PadLeft(8, '0');
            var stream = CompoundFile.RootStorage.TryGetStream(name);
            while (stream != null)
            {
                index += 1;
                name = propertyTag.Name + "-" + index.ToString().PadLeft(8, '0');
                stream = CompoundFile.RootStorage.TryGetStream(name);
                CompoundFile.RootStorage.Delete(name);
            }

            index = 0;

            foreach (var value in values)
            {
                name = propertyTag.Name + "-" + index.ToString().PadLeft(8, '0');
                stream = CompoundFile.RootStorage.TryGetStream(name);
                if (stream != null)
                {
                    switch (propertyTag.Type)
                    {
                        case PropertyType.PT_MV_STRING8:
                        case PropertyType.PT_MV_UNICODE:
                            stream.SetData(Encoding.Unicode.GetBytes(value));
                            break;

                        default:
                            throw new MWInvalidProperty("The property is not of the type PT_MV_STRING8 or PT_MV_UNICODE");
                    }
                }
                else
                {
                    stream = CompoundFile.RootStorage.AddStream(propertyTag.Name);
                    stream.SetData(Encoding.Unicode.GetBytes(value));
                }

                index += 1;
            }
        }
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}