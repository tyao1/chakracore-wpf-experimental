using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChakraHost.Hosting;
using System.IO;
using ChakraCoreBrdige;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using ChakraCoreBrdige.Manager;

namespace ChakraCoreBridge.Bridge
{
    class Bridge
    {
        private static JavaScriptSourceContext currentSourceContext = JavaScriptSourceContext.FromIntPtr(IntPtr.Zero);

        // Functions to inject to Chakra
        private static JavaScriptValue LogRaw(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            Console.Write("");
            for (uint index = 1; index < argumentCount; index++)
            {
                if (index > 1)
                {
                    Console.Write(" ");
                }

                Console.Write(arguments[index].ConvertToString().ToString());
            }

            Console.WriteLine();

            return JavaScriptValue.Invalid;
        }

        private static Canvas _container;
        public static void SetInterface(MainWindow mainWindow)
        {
            Canvas canv = new Canvas();
            // Grid grid = new Grid();
            mainWindow.Content = canv;
            // Button button1 = new Button();
            // button1.Content = "Text";
            // canv.Children.Add(button1);

            _container = canv;
        }
        // private static TaskScheduler _UIScheduler;
        private static Dispatcher _dispatcher;
        private static JavaScriptValue Log(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            Console.Write("Log: ");
            return LogRaw(callee, isConstructCall, arguments, argumentCount, callbackData);
        }
        private static JavaScriptValue Info(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            Console.Write("Info: ");
            return LogRaw(callee, isConstructCall, arguments, argumentCount, callbackData);
        }
        private static JavaScriptValue Warn(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            Console.Write("Warn: ");
            return LogRaw(callee, isConstructCall, arguments, argumentCount, callbackData);
        }
        private static JavaScriptValue Error(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            Console.Write("Error: ");
            return LogRaw(callee, isConstructCall, arguments, argumentCount, callbackData);
        }
        private static JavaScriptNativeFunction logDelegate = Log;
        private static JavaScriptNativeFunction infoDelegate = Info;
        private static JavaScriptNativeFunction warnDelegate = Warn;
        private static JavaScriptNativeFunction errorDelegate = Error;

        private static JavaScriptValue SimpleCalc(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            JavaScriptValue doubleResult = JavaScriptValue.Invalid;

            ThreadPool.QueueUserWorkItem((object state) =>
            {
                try
                {
                    double left = arguments[1].ToDouble();
                    double right = arguments[2].ToDouble();
                    for (int ind = 0; ind < 100000; ind++)
                    {
                        doubleResult = JavaScriptValue.FromDouble(left++ + right++ + 1);
                    }
                    Console.Write(doubleResult.ToDouble());
                }
                catch (JavaScriptException e)
                {
                    Console.WriteLine("JS Boom");
                    Console.WriteLine(e.StackTrace);
                }
            });

           
            return doubleResult;
        }
        private static JavaScriptNativeFunction simpleCalcDelegate = SimpleCalc;

        
      
        // Helper utils
        private static void DefineHostCallback(JavaScriptValue sourceObject, string callbackName, JavaScriptNativeFunction callback, IntPtr callbackData)
        {
            JavaScriptPropertyId propertyId = JavaScriptPropertyId.FromString(callbackName);
            JavaScriptValue function = JavaScriptValue.CreateFunction(callback, callbackData);
            sourceObject.SetProperty(propertyId, function, true);
        }

        private static void PatchConsole(JavaScriptValue globalObject)
        {
            JavaScriptValue console = JavaScriptValue.CreateObject();
            // set properties on console
            DefineHostCallback(console, "log", logDelegate, IntPtr.Zero);
            DefineHostCallback(console, "info", infoDelegate, IntPtr.Zero);
            DefineHostCallback(console, "warn", warnDelegate, IntPtr.Zero);
            DefineHostCallback(console, "error", errorDelegate, IntPtr.Zero);

            JavaScriptPropertyId consoleId = JavaScriptPropertyId.FromString("console");
            globalObject.SetProperty(consoleId, console, true);
        }


        // Functions Collections
        private static int _funcId = 0;
        private static Dictionary<int, Action<JavaScriptValue[], Action<JavaScriptValue, object[], JavaScriptValue>, JavaScriptValue, JavaScriptValue>> nativeFunctions = new Dictionary<int, Action<JavaScriptValue[], Action<JavaScriptValue, object[], JavaScriptValue>, JavaScriptValue, JavaScriptValue>>();
        // Call native functions
        private static JavaScriptValue callNative(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            if (argumentCount < 2)
            {
                throw new Exception("Arguments incorrect");
            }
            
            try
            {
                var moduleId = Convert.ToInt32(arguments[1].ToDouble());
                var function = nativeFunctions[moduleId];
                if (function == null) { throw new Exception("Cannot find module:" + moduleId); }
                JavaScriptValue callbackId = arguments[2]; // the id of the registed callback
                // if (!callbackId.Equals(JavaScriptValue.Undefined))
                //{
                    // start native actions in a new thread
                    ThreadPool.QueueUserWorkItem((object state) =>
                    {
                        function(arguments, callJS, callbackId, callee);
                    });
                //}
                
            }
            catch (JavaScriptException e)
            {
                Console.WriteLine("JS Boom");
                Console.WriteLine(e.StackTrace);
            }
            return JavaScriptValue.True;
        }
        private static JavaScriptNativeFunction callNativeDelegate = callNative;

        // bridge and global Object
        private static JavaScriptValue _jsBridge;
        private static JavaScriptValue _global;
        public static JavaScriptValue Global
        {
            get
            {
                return _global;
            }
        }

        // call javascript
        private static void callJS(JavaScriptValue callBackId, object[] results, JavaScriptValue callee)
        {
            queue.Add(() =>
            {
                var pass = new JavaScriptValue[2 + results.Length];
                pass[0] = callee;
                pass[1] = callBackId;
                for (int i = 0; i < results.Length; i++)
                {
                    var parameter = results[i];
                    var parameterType = parameter.GetType().Name;
                    switch (parameterType)
                    {
                        case "Int32":
                            pass[i+2] = (JavaScriptValue.FromInt32((int)parameter));
                            break;
                        case "Double":
                            pass[i + 2] = (JavaScriptValue.FromDouble((double)parameter));
                            break;
                        case "Boolean":
                            pass[i + 2] = (JavaScriptValue.FromBoolean((bool)parameter));
                            break;
                        case "String":
                            pass[i + 2] = (JavaScriptValue.FromString((string)parameter));
                            break;
                        case "JavaScriptValue":
                            pass[i + 2] = (JavaScriptValue)parameter;
                            break;
                        default:
                            Console.WriteLine("Not supported type: " + parameterType);
                            pass[i + 2] = JavaScriptValue.Null;
                            break;
                    }
                }
                _jsBridge.CallFunction(pass);
            });

        }


        private static int _componentsKey = 0;
        private static Dictionary<int, Func<Dispatcher, JavaScriptValue[], Canvas, dynamic>> components = new Dictionary<int, Func<Dispatcher, JavaScriptValue[], Canvas, dynamic>>();

        private static Dictionary<int, dynamic> _mountedNodes = new Dictionary<int, dynamic>();
        private static JavaScriptValue mount(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {
            
            // var controlName = arguments[1].ToString();
            var controlId = Convert.ToInt32(arguments[1].ToDouble());
            var componentId = Convert.ToInt32(arguments[3].ToDouble());
            Console.WriteLine("mount:" + controlId);
            var func = components[controlId];
            if (func != null)
            {
                var parent = _container;
                if (argumentCount > 4 && !arguments[4].Equals(JavaScriptValue.Undefined))
                {
                    var parentId = Convert.ToInt32(arguments[4].ToDouble());
                    parent = _mountedNodes[parentId];
                }
                var elem = func(_dispatcher, arguments, parent);
                _mountedNodes.Add(componentId, elem);
            }

            return JavaScriptValue.Invalid;
        }
        private static JavaScriptNativeFunction mountDelegate = mount;

        private static JavaScriptValue unmount(JavaScriptValue callee, bool isConstructCall, JavaScriptValue[] arguments, ushort argumentCount, IntPtr callbackData)
        {

            // var controlName = arguments[1].ToString();
            var componentId = Convert.ToInt32(arguments[2].ToDouble());
            int parentId;
            if (!arguments[1].Equals(JavaScriptValue.Undefined))
            {
                parentId = Convert.ToInt32(arguments[1].ToDouble());
                var parent = _mountedNodes[parentId];
                _dispatcher.Invoke(() =>
                {
                    parent.Children.Remove(_mountedNodes[componentId]);
                });
            } else
            {
                _dispatcher.Invoke(() =>
                {
                    _container.Children.Remove(_mountedNodes[componentId]);
                });
            }

            
            _mountedNodes.Remove(componentId);
            return JavaScriptValue.Invalid;
        }
        private static JavaScriptNativeFunction unmountDelegate = unmount;


        private static Dictionary<int, string> _componentMap = new Dictionary<int, string>();
        public static void AddComponent(Func<Dispatcher, JavaScriptValue[], Canvas, dynamic> function, string name)
        {
            _componentMap.Add(_componentsKey, name);
            components.Add(_componentsKey++, function);
        }


        private static Dictionary<int, string> _functionMap = new Dictionary<int, string>();
        public static void AddNativeFunction(Action<JavaScriptValue[], Action<JavaScriptValue, object[], JavaScriptValue>, JavaScriptValue, JavaScriptValue> function, string name)
        {
            _functionMap.Add(_funcId, name);
            nativeFunctions.Add(_funcId++, function);
        }

        public static JavaScriptContext context = JavaScriptContext.Invalid;
        // The main entry point for the host.

        private static LimitedConcurrencyLevelTaskScheduler taskScheduler = new LimitedConcurrencyLevelTaskScheduler(1);

        // public static TaskFactory taskFactory = new TaskFactory(taskScheduler);
        // public static TaskFactory nativeTaskFactory = new TaskFactory();
        public static BlockingCollection<Action> queue;

        public static void RunScriptInNewThread(Dispatcher scheduler)
        {
            _dispatcher = scheduler;
            queue = new BlockingCollection<Action>();
            var asyncAction = new Thread(new ThreadStart(
                () =>
                {
                    // JavaScriptContext.Current = context;

                    while (true)
                    {
                        var action = queue.Take();
                        try {
                            // JavaScriptContext.Current = context;
                            action();
                            // JavaScriptContext.Current = JavaScriptContext.Invalid;
                        }
                        catch (Exception ex) {
                            Console.WriteLine(ex.StackTrace);
                        }
                    }

                    // JavaScriptContext.Current = JavaScriptContext.Invalid;
                }));

            // Enqueues work
            queue.Add(RunScript);
            asyncAction.Start();
            // taskFactory.StartNew(RunScript);
        }
        public static void RunScript()
        {
            int returnValue = 1;
            try
            {
                //
                // Create the runtime. We're only going to use one runtime for this host.
                //

                using (JavaScriptRuntime runtime = JavaScriptRuntime.Create())
                {
                    //
                    // Similarly, create a single execution context. Note that we're putting it on the stack here,
                    // so it will stay alive through the entire run.
                    //

                    //
                    // Now set the execution context as being the current one on this thread.
                    //
                    context = runtime.CreateContext();
                    JavaScriptContext.Current = context;

                    //
                    // Set the property.
                    //

                    using (new JavaScriptContext.Scope(context))
                    {

                        // set the global objects
                        JavaScriptValue bridge = JavaScriptValue.CreateObject();
                        JavaScriptValue global = JavaScriptValue.GlobalObject;
                        JavaScriptPropertyId bridgePropertyId = JavaScriptPropertyId.FromString("bridge");
                        global.SetProperty(bridgePropertyId, bridge, true);
                        PatchConsole(global);

                        JavaScriptValue simpleCalcFunc = JavaScriptValue.CreateFunction(simpleCalcDelegate, IntPtr.Zero);
                        bridge.SetProperty(JavaScriptPropertyId.FromString("calc"), simpleCalcFunc, true);
                        JavaScriptValue callNativeFunc = JavaScriptValue.CreateFunction(callNativeDelegate, IntPtr.Zero);
                        bridge.SetProperty(JavaScriptPropertyId.FromString("callNative"), callNativeFunc, true);
                        DefineHostCallback(bridge, "mount", mountDelegate, IntPtr.Zero);
                        DefineHostCallback(bridge, "unmount", unmountDelegate, IntPtr.Zero);

                        // set components map
                        var _injectComponentsMap = JavaScriptValue.CreateObject();
                        var _injectFunctionMap = JavaScriptValue.CreateObject();
                        foreach (var pair in _componentMap)
                        {
                            _injectComponentsMap.SetProperty(JavaScriptPropertyId.FromString(pair.Value), JavaScriptValue.FromInt32(pair.Key), true);
                        }

                        foreach(var pair in _functionMap)
                        {
                            _injectFunctionMap.SetProperty(JavaScriptPropertyId.FromString(pair.Value), JavaScriptValue.FromInt32(pair.Key), true);

                        }
                        bridge.SetProperty(JavaScriptPropertyId.FromString("components"), _injectComponentsMap, true);
                        bridge.SetProperty(JavaScriptPropertyId.FromString("functions"), _injectFunctionMap, true);



                        // set window
                        global.SetProperty(JavaScriptPropertyId.FromString("window"), global, true);
                        //
                        // Load the script from the disk.
                        //

                        string script = File.ReadAllText("C:/test.js");
                        Console.WriteLine(script);
                        //
                        // Inject JS Bridge
                        //
                        JavaScriptContext.RunScript(
    "callbackCount = 0;"+
    "                    callbacks = [];" +
    "                    bridge.callJS = function(id, result) {" +
    "console.log('called js', id, result);"+
    "console.log(callbacks.length, callbacks[id]);"+
    "                        var callback = callbacks[id];" +
    "                        callback(result);" +
    "                        callbacks[id] = null;" +
    "                    }");
                        _jsBridge = bridge.GetProperty(JavaScriptPropertyId.FromString("callJS"));
                        _global = global;
                        JavaScriptValue result;
                        try
                        {
                            result = JavaScriptContext.RunScript(script, currentSourceContext++, "C:\test.js");
                        }
                        catch (JavaScriptScriptException e)
                        {
                            Console.Write("fkerrorerror");
                            JavaScriptPropertyId messageName = JavaScriptPropertyId.FromString("message");
                            if (e.Error.HasProperty(messageName))
                            {
                                JavaScriptValue messageValue = e.Error.GetProperty(messageName);
                                string message = messageValue.ConvertToString().ToString();
                                Console.Error.WriteLine("chakrahost: exception: {0}", message);
                            } else
                            {
                                Console.WriteLine("JS Boom");
                            }
                            return;
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine("chakrahost: failed to run script: {0}", e.Message);
                            return;
                        }

                        //
                        // Convert the return value.
                        //

                        JavaScriptValue numberResult = result.ConvertToNumber();
                        double doubleResult = numberResult.ToDouble();
                        returnValue = (int)doubleResult;
                    }
                }
                JavaScriptContext.Current = JavaScriptContext.Invalid;

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("chakrahost: fatal error: internal error: {0}.", e.Message);
            }

            Console.WriteLine(returnValue);
            return;
        }
    }
}
