using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChakraCoreBridge.Bridge;
using ChakraHost.Hosting;
using System.Windows.Threading;
using System.Net.Http;
using System.Net;

namespace ChakraCoreBrdige
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static Window dis;

        private static void naive(JavaScriptValue[] arguments, Action<JavaScriptValue, object[], JavaScriptValue> callJS, JavaScriptValue callBackId, JavaScriptValue callee)
        {
            var result = new object[1];
            result[0] = arguments[3];
            callJS(callBackId, result, callee);
        }

        private static void setTitle(JavaScriptValue[] arguments, Action<JavaScriptValue, object[], JavaScriptValue> callJS, JavaScriptValue callBackId, JavaScriptValue callee)
        {
            var val = arguments[3].ToString();
            dis.Dispatcher.Invoke(() =>
            {
                dis.Title = val;
            });
        }

        private static void request(JavaScriptValue[] arguments, Action<JavaScriptValue, object[], JavaScriptValue> callJS, JavaScriptValue callBackId, JavaScriptValue callee)
        {
            var result = new object[1];
            var src = arguments[3];
            var parsed = src.ToString();
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(parsed).Result;

                    response.EnsureSuccessStatusCode();
                    var res = response.Content.ReadAsStringAsync().Result;
                    result[0] = res;
                } catch
                {
                    result[0] = "[]";
                }
            }
            
            callJS(callBackId, result, callee);
        }

        private static Button MyButton(Dispatcher dispatcher, JavaScriptValue[] arguments, Canvas parent)
        {
            var props = arguments[2];
            var text = props.GetProperty(JavaScriptPropertyId.FromString("text")).ToString();
            double top = 0;
            double left = 0;
            double width = -1;
            double height = -1;
            var rawLeft = props.GetProperty(JavaScriptPropertyId.FromString("left"));
            if (!rawLeft.Equals(JavaScriptValue.Undefined)) left = rawLeft.ToDouble();
            var rawTop = props.GetProperty(JavaScriptPropertyId.FromString("top"));
            if (!rawTop.Equals(JavaScriptValue.Undefined)) top = rawTop.ToDouble();

            var rawWidth = props.GetProperty(JavaScriptPropertyId.FromString("width"));
            if (!rawWidth.Equals(JavaScriptValue.Undefined)) width = rawWidth.ToDouble();

            var rawHeight = props.GetProperty(JavaScriptPropertyId.FromString("height"));
            if (!rawHeight.Equals(JavaScriptValue.Undefined)) height = rawHeight.ToDouble();

            var onClick = props.GetProperty(JavaScriptPropertyId.FromString("onClick"));
            var hasOnClick = !onClick.Equals(JavaScriptValue.Undefined);

            return dispatcher.Invoke(new Func<Button>(() =>
            {
                var btn = new Button();
                btn.Content = text;
                btn.VerticalAlignment = VerticalAlignment.Top;
                btn.SetValue(Canvas.TopProperty, top);
                btn.SetValue(Canvas.LeftProperty, left);
                if (width > -1)
                {
                    btn.Width = width;
                }
                if (height > -1)
                {
                    btn.Height = height;
                }
                if (hasOnClick)
                {
                    Console.WriteLine("Hook Event");
                    btn.Click += (object sender, RoutedEventArgs e) =>
                    {
                        Console.WriteLine("NATIVE CLICKED");
                        Bridge.queue.Add(() =>
                        {
                            Console.WriteLine("?? CLICKED");

                            //using (new JavaScriptContext.Scope(Bridge.context))
                            //{
                            // JavaScriptContext.Current = Bridge.context;
                            Bridge.queue.Add(() =>
                            {
                                var pass = new JavaScriptValue[1];
                                pass[0] = Bridge.Global;
                                onClick.CallFunction(pass);
                            });
                                
                            // JavaScriptContext.Current = JavaScriptContext.Invalid;
                           // }
                                

                        });
                    };
                }
                parent.Children.Add(btn);
                return btn;
            }));
        }


        private static Label MyLabel(Dispatcher dispatcher, JavaScriptValue[] arguments, Canvas parent)
        {
            var props = arguments[2];
            string text = "";
            double fontSize = 12;
            
            var rawText = props.GetProperty(JavaScriptPropertyId.FromString("text"));
            if (!rawText.Equals(JavaScriptValue.Undefined)) text = rawText.ToString();
            var rawFontSize = props.GetProperty(JavaScriptPropertyId.FromString("fontSize"));
            if (!rawFontSize.Equals(JavaScriptValue.Undefined)) fontSize = rawFontSize.ToDouble();

            double top = 0;
            double left = 0;
            double width = -1;
            double height = -1;
            var rawLeft = props.GetProperty(JavaScriptPropertyId.FromString("left"));
            if (!rawLeft.Equals(JavaScriptValue.Undefined)) left = rawLeft.ToDouble();
            var rawTop = props.GetProperty(JavaScriptPropertyId.FromString("top"));
            if (!rawTop.Equals(JavaScriptValue.Undefined)) top = rawTop.ToDouble();

            var rawWidth = props.GetProperty(JavaScriptPropertyId.FromString("width"));
            if (!rawWidth.Equals(JavaScriptValue.Undefined)) width = rawWidth.ToDouble();

            var rawHeight = props.GetProperty(JavaScriptPropertyId.FromString("height"));
            if (!rawHeight.Equals(JavaScriptValue.Undefined)) height = rawHeight.ToDouble();

            var onClick = props.GetProperty(JavaScriptPropertyId.FromString("onClick"));
            var hasOnClick = !onClick.Equals(JavaScriptValue.Undefined);

            return dispatcher.Invoke(new Func<Label>(() =>
            {
                var label = new Label();
                label.Content = text;
                label.FontSize = fontSize;
                label.VerticalAlignment = VerticalAlignment.Top;
                
                label.SetValue(Canvas.TopProperty, top);
                label.SetValue(Canvas.LeftProperty, left);

                if (hasOnClick)
                {
                    Console.WriteLine("Hook Event");

                    label.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) =>
                    {
                        Console.WriteLine("NATIVE CLICKED");
                        Bridge.queue.Add(() =>
                        {
                            Console.WriteLine("?? CLICKED");
                            Bridge.queue.Add(() =>
                            {
                                var pass = new JavaScriptValue[1];
                                pass[0] = Bridge.Global;
                                onClick.CallFunction(pass);
                            });
                        });
                    };
                }
                if (width > -1)
                {
                    label.Width = width;
                }
                if (height > -1)
                {
                    label.Height = height;
                }
                parent.Children.Add(label);
                return label;
            }));
        }

        private static Image MyImage(Dispatcher dispatcher, JavaScriptValue[] arguments, Canvas parent)
        {
            var props = arguments[2];
            var src = props.GetProperty(JavaScriptPropertyId.FromString("src")).ToString();
            double top = 0;
            double left = 0;
            double width = -1;
            double height = -1;
            var rawLeft = props.GetProperty(JavaScriptPropertyId.FromString("left"));
            if (!rawLeft.Equals(JavaScriptValue.Undefined)) left = rawLeft.ToDouble();
            var rawTop = props.GetProperty(JavaScriptPropertyId.FromString("top"));
            if (!rawTop.Equals(JavaScriptValue.Undefined)) top = rawTop.ToDouble();

            var rawWidth = props.GetProperty(JavaScriptPropertyId.FromString("width"));
            if (!rawWidth.Equals(JavaScriptValue.Undefined)) width = rawWidth.ToDouble();

            var rawHeight = props.GetProperty(JavaScriptPropertyId.FromString("height"));
            if (!rawHeight.Equals(JavaScriptValue.Undefined)) height = rawHeight.ToDouble();
            return dispatcher.Invoke(new Func<Image>(() =>
            {
                var image = new Image();
                image.Source = new BitmapImage(new Uri(src, UriKind.Absolute));
                image.VerticalAlignment = VerticalAlignment.Top;
                image.SetValue(Canvas.TopProperty, top);
                image.SetValue(Canvas.LeftProperty, left);
                if (width > -1)
                {
                    image.Width = width;
                }
                if (height > -1)
                {
                    image.Height = height;
                }
                parent.Children.Add(image);
                return image;
            }));
        }

        private static Canvas MyCanvas(Dispatcher dispatcher, JavaScriptValue[] arguments, Canvas parent)
        {
            var props = arguments[2];

            double top = 0;
            double left = 0;
            double width = -1;
            double height = -1;
            var rawLeft = props.GetProperty(JavaScriptPropertyId.FromString("left"));
            if (!rawLeft.Equals(JavaScriptValue.Undefined)) left = rawLeft.ToDouble();
            var rawTop = props.GetProperty(JavaScriptPropertyId.FromString("top"));
            if (!rawTop.Equals(JavaScriptValue.Undefined)) top = rawTop.ToDouble();

            var rawWidth = props.GetProperty(JavaScriptPropertyId.FromString("width"));
            if (!rawWidth.Equals(JavaScriptValue.Undefined)) width = rawWidth.ToDouble();

            var rawHeight = props.GetProperty(JavaScriptPropertyId.FromString("height"));
            if (!rawHeight.Equals(JavaScriptValue.Undefined)) height = rawHeight.ToDouble();
            // var props = arguments[2];
            return dispatcher.Invoke(new Func<Canvas>(() =>
            {
                Canvas canv = new Canvas();
                canv.SetValue(Canvas.TopProperty, top);
                canv.SetValue(Canvas.LeftProperty, left);
                if (width > -1)
                {
                    canv.Width = width;
                }
                if (height > -1)
                {
                    canv.Height = height;
                }
                parent.Children.Add(canv);
                return canv;
            }));
        }

        private static WebBrowser MyWebView(Dispatcher dispatcher, JavaScriptValue[] arguments, Canvas parent)
        {
            var props = arguments[2];

            double top = 0;
            double left = 0;
            double width = -1;
            double height = -1;
            var rawLeft = props.GetProperty(JavaScriptPropertyId.FromString("left"));
            if (!rawLeft.Equals(JavaScriptValue.Undefined)) left = rawLeft.ToDouble();
            var rawTop = props.GetProperty(JavaScriptPropertyId.FromString("top"));
            if (!rawTop.Equals(JavaScriptValue.Undefined)) top = rawTop.ToDouble();

            var rawWidth = props.GetProperty(JavaScriptPropertyId.FromString("width"));
            if (!rawWidth.Equals(JavaScriptValue.Undefined)) width = rawWidth.ToDouble();

            var rawHeight = props.GetProperty(JavaScriptPropertyId.FromString("height"));
            if (!rawHeight.Equals(JavaScriptValue.Undefined)) height = rawHeight.ToDouble();

            var src = props.GetProperty(JavaScriptPropertyId.FromString("src")).ToString();

            // var props = arguments[2];
            return dispatcher.Invoke(new Func<WebBrowser>(() =>
            {
                WebBrowser canv = new WebBrowser();
                canv.Source = new Uri(src, UriKind.Absolute);
                canv.SetValue(Canvas.TopProperty, top);
                canv.SetValue(Canvas.LeftProperty, left);
                if (width > -1)
                {
                    canv.Width = width;
                }
                if (height > -1)
                {
                    canv.Height = height;
                }
                parent.Children.Add(canv);
                return canv;
            }));
        }

        public class ScrollableCanvasControl : Canvas
        {
            static ScrollableCanvasControl()
            {
                DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrollableCanvasControl), new FrameworkPropertyMetadata(typeof(ScrollableCanvasControl)));
            }

            protected override Size MeasureOverride(Size constraint)
            {
                double bottomMost = 0d;
                double rightMost = 0d;

                foreach (object obj in Children)
                {
                    FrameworkElement child = obj as FrameworkElement;

                    if (child != null)
                    {
                        child.Measure(constraint);

                        bottomMost = Math.Max(bottomMost, GetTop(child) + child.DesiredSize.Height);
                        rightMost = Math.Max(rightMost, GetLeft(child) + child.DesiredSize.Width);
                    }
                }
                return new Size(rightMost, bottomMost);
            }
        }
        private static ScrollableCanvasControl MyScrollView(Dispatcher dispatcher, JavaScriptValue[] arguments, Canvas parent)
        {
            var props = arguments[2];

            double top = 0;
            double left = 0;
            double width = -1;
            double height = -1;
            var rawLeft = props.GetProperty(JavaScriptPropertyId.FromString("left"));
            if (!rawLeft.Equals(JavaScriptValue.Undefined)) left = rawLeft.ToDouble();
            var rawTop = props.GetProperty(JavaScriptPropertyId.FromString("top"));
            if (!rawTop.Equals(JavaScriptValue.Undefined)) top = rawTop.ToDouble();

            var rawWidth = props.GetProperty(JavaScriptPropertyId.FromString("width"));
            if (!rawWidth.Equals(JavaScriptValue.Undefined)) width = rawWidth.ToDouble();

            var rawHeight = props.GetProperty(JavaScriptPropertyId.FromString("height"));
            if (!rawHeight.Equals(JavaScriptValue.Undefined)) height = rawHeight.ToDouble();
            // var props = arguments[2];
            return dispatcher.Invoke(new Func<ScrollableCanvasControl>(() =>
            {
                ScrollViewer scrV = new ScrollViewer();
                ScrollableCanvasControl canv = new ScrollableCanvasControl();
                scrV.Content = canv;
                scrV.SetValue(Canvas.TopProperty, top);
                scrV.SetValue(Canvas.LeftProperty, left);
                if (width > -1)
                {
                    scrV.Width = width;
                }
                if (height > -1)
                {
                    scrV.Height = height;
                }
                parent.Children.Add(scrV);
                return canv;
            }));
        }

        public MainWindow()
        {
            dis = this;
            InitializeComponent();

            Bridge.AddNativeFunction(naive, "naive");

            Bridge.AddNativeFunction(request, "request");

            Bridge.AddNativeFunction(setTitle, "setTitle");

            Bridge.AddComponent(MyWebView, "WebView");
            Bridge.AddComponent(MyCanvas, "View");
            Bridge.AddComponent(MyScrollView, "ScrollView");

            Bridge.AddComponent(MyButton, "Button");
            Bridge.AddComponent(MyLabel, "Text");
            Bridge.AddComponent(MyImage, "Image");

            Bridge.SetInterface(this);
            Bridge.RunScriptInNewThread(this.Dispatcher);
        }
    }
}
