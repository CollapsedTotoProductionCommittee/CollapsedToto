using System;
using System.Reflection;
using Nancy;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Linq;

namespace CollapsedToto
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PrefixAttribute : Attribute
    {
        public PrefixAttribute(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RoutingAttribute : Attribute
    {
        public RoutingAttribute(string path, RoutingType routing)
        {
            Path = path;
            Routing = routing;
        }

        public string Path { get; set; }
        public RoutingType Routing { get; set; }
        public enum RoutingType
        {
            GET, POST
        }
    }

    public class GetAttribute : RoutingAttribute
    {
        public GetAttribute(string path)
            : base(path, RoutingType.GET)
        {
        }
    }

    public class PostAttribute : RoutingAttribute
    {
        public PostAttribute(string path)
            : base(path, RoutingType.POST)
        {
        }
    }

    public class BaseModule : NancyModule
    {
        public BaseModule()
        {
            if (this.GetType() == typeof(BaseModule))
            {
                return;
            }

            PrefixAttribute prefixAttrib = this.GetType().GetCustomAttribute<PrefixAttribute>(false);
            string prefix = null;
            if (prefixAttrib != null)
            {
                prefix = prefixAttrib.Prefix;
            }

            foreach(var method in this.GetType().GetMethods())
            {
                RoutingAttribute attrib = method.GetCustomAttribute<RoutingAttribute>(true);
                if (attrib == null)
                {
                    continue;
                }

                string path;
                if (prefix == null)
                {
                    path = attrib.Path;
                }
                else
                {
                    path = string.Format("{0}{1}", prefix, attrib.Path);
                }

                bool isAsync = method.GetCustomAttribute<AsyncStateMachineAttribute>(false) != null;
                dynamic func = null;

                try
                {
                    func = method.CreateDelegate(Expression.GetDelegateType(
                       (from parameter in method.GetParameters() select parameter.ParameterType)
                            .Concat(new[] { method.ReturnType })
                            .ToArray()), this);
                }
                catch
                {
                    continue;
                }
                if (func == null)
                {
                    continue;
                }

                switch (attrib.Routing)
                {   
                    case RoutingAttribute.RoutingType.GET:
                        if (isAsync)
                            
                        {
                            Get[path, true] = func;
                        }
                        else
                        {
                            Get[path] = func;
                        }
                        break;
                    case RoutingAttribute.RoutingType.POST:
                        if (isAsync)
                        {
                            Post[path, true] = func;
                        }
                        else
                        {
                            Post[path] = func;
                        }
                        break;
                }
            }
        }
    }
}

