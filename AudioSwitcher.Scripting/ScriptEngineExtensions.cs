﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AudioSwitcher.AudioApi;
using AudioSwitcher.Scripting.Libraries;
using Jurassic;
using Jurassic.Library;

namespace AudioSwitcher.Scripting
{
    public static class ScriptEngineExtensions
    {
        public static AudioSwitcherLibrary AddAudioSwitcherLibrary(this ScriptEngine engine, AudioController controller)
        {
            return AddLibrary(engine, new AudioSwitcherLibrary(engine, controller));
        }

        public static CoreLibrary AddCoreLibrary(this ScriptEngine engine)
        {
            return AddLibrary(engine, new CoreLibrary(engine));
        }

        /// <summary>
        ///     Add the library to the current lua controller
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="library"></param>
        /// <returns></returns>
        public static bool AddLibrary(this ScriptEngine engine, IJavaScriptLibrary library)
        {
            library.Add(engine);
            return true;
        }

        /// <summary>
        ///     Add the library to the current lua controller
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="library"></param>
        /// <returns></returns>
        public static T AddLibrary<T>(this ScriptEngine engine, T library) where T : IJavaScriptLibrary
        {
            library.Add(engine);
            return library;
        }

        /// <summary>
        ///     Removes the library import from the current lua controller
        /// </summary>
        /// <param name="engine"></param>
        /// <param name="library"></param>
        /// <returns></returns>
        public static bool RemoveLibrary(this ScriptEngine engine, IJavaScriptLibrary library)
        {
            library.Remove(engine);
            return true;
        }

        public static ArrayInstance EnumerableToArray(this ScriptEngine engine, IEnumerable<object> collection)
        {
            return engine.Array.New(collection.ToArray());
        }

        /// <summary>
        /// Adds a function to an object instance, i.e library
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="name"></param>
        /// <param name="getterFunc"></param>
        /// <remarks>Seriously Jurassic... You think nobody will lazily add functions to objects?</remarks>
        public static void AddFunction<T>(this ObjectInstance instance, string name, Func<T> getterFunc)
        {
            var ctor =
                typeof(ClrFunction).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new [] { typeof(ObjectInstance), typeof(Delegate), typeof(string), typeof(int) },
                null);

            FunctionInstance func = (ClrFunction)ctor.Invoke(new object[] { instance.Engine.Function.Prototype, getterFunc, null, null });
            instance.SetPropertyValue(name, func, true);
        }
    }
}