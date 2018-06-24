using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuayUpgradeTool.Wrappers
{
    public class NetToolWrapper
    {
        private readonly NetTool _netTool;        

        public NetToolWrapper(NetTool netTool)
        {
            _netTool = netTool;
        }

        #region Properties

        public IPropertyType GetProperty<IPropertyType>(string propertyName)
        {
            try
            {
                var property = _netTool.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                return (IPropertyType)property.GetValue(_netTool);
            }
            catch (Exception e)
            {
                DebugUtils.Log($"Failed to get property with name {propertyName}");
                DebugUtils.LogException(e);
                return default(IPropertyType);
            }
        }

        public void SetProperty<IPropertyType>(string propertyName, IPropertyType value)
        {
            try
            {
                var property = _netTool.GetType().GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                property.SetValue(_netTool, value);
            }
            catch (Exception e)
            {
                DebugUtils.Log($"Failed to set property with name {propertyName} and value {value}");
                DebugUtils.LogException(e);
            }
        }

        #endregion

        #region Methods

        private readonly MethodInfo OnToolGUIMethod = typeof(NetTool).GetMethod("OnToolGUI", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private readonly MethodInfo CreateNodeMethod = typeof(NetTool).GetMethod("CreateNode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        private readonly MethodInfo GetSecondaryControlPointsMethod = typeof(NetTool).GetMethod("GetSecondaryControlPoints", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        private readonly MethodInfo CreateNodeImplMethod = typeof(NetTool).GetMethod("CreateNodeImpl", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, Type.DefaultBinder, new[] { typeof(NetInfo), typeof(bool), typeof(bool), typeof(NetTool.ControlPoint), typeof(NetTool.ControlPoint), typeof(NetTool.ControlPoint) }, null);

        public void OnToolGUI(UnityEngine.Event e)
        {
            DebugUtils.Log($"Calling OnToolGui on {_netTool} with event {e}");
            OnToolGUIMethod.Invoke(_netTool, new object[] { e });
        }

        public IEnumerator<bool> CreateNode(bool switchDirection)
        {
            DebugUtils.Log($"Calling CreateNode on {_netTool} with switchDirection {switchDirection}");
            return (IEnumerator<bool>)CreateNodeMethod.Invoke(_netTool, new object[] { switchDirection });
        }

        public bool GetSecondaryControlPoints(NetInfo info, ref NetTool.ControlPoint startPoint, ref NetTool.ControlPoint middlePoint, ref NetTool.ControlPoint endPoint)
        {
            DebugUtils.Log($"Calling GetSecondaryControlPoints on {_netTool}.");
            return (bool)GetSecondaryControlPointsMethod.Invoke(_netTool, new object[] { info, startPoint, middlePoint, endPoint });
        }

        public bool CreateNodeImpl(NetInfo info, bool needMoney, bool switchDirection, NetTool.ControlPoint startPoint, NetTool.ControlPoint middlePoint, NetTool.ControlPoint endPoint)
        {
            DebugUtils.Log($"Calling CreateNodeImpl on {_netTool}.");
            return (bool)CreateNodeImplMethod.Invoke(_netTool, new object[] { info, needMoney, switchDirection, startPoint, middlePoint, endPoint });
        }

        #endregion
    }
}
