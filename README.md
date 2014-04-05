SessionExtentionStore
======================

一个基于Redis的Session存储扩展方案，解决ASP.NET中Session的局限性和跨应用程序使用的局限性

原生的Session解决方案存在着跨应用程序的困难，扩展性的困难，而SessionExtentionStore方案致力于解决这个问题。
这是一个简单的处理方案，使用了这个解决方案，您能将多个应用间数据交互交由SessionExtentionStore解决，
在使用共享Session提供SSO的解决方案中，尤其有用。

我采用的是类Session的处理方式，和SessionId绑定到了一起，这样就能依赖于Session的机制将用户和扩展绑定到了一起。

使用方法：
配置web.config,增加以下配置节点：
```xml
 <system.web>
    <httpModules>
      <add name="SessionExtentionStore" type="SessionExtentionStore.UpdateTTL"/>
    </httpModules>
 <system.web> 
 
  <appSettings>
     <add key="SessionExtention" value="127.0.0.1"/>
  </appSettings>
 ```
  
  要是MVC环境，则建立一个Controller父类，派生自Controller类，若是WebForm则建立一个父类派生自System.Web.UI.Page，
  父类加入以下属性定义：
  ```csharp
        private StoreContainer _store;
        public StoreContainer Store
        {
            get
            {
                if (!string.IsNullOrEmpty(Session.SessionID))
                {
                    Session["__TempCreate__"] = 1;
                    return new StoreContainer(Session.SessionID);
                }
                return _store ?? (_store = new StoreContainer(Session.SessionID));
            }
        }
  ```
        
  其他所有页面都派生自这两个父类，然后均可以使用Store属性进行数据存储，与使用Session的方式一样。
  因为存储内容当中带有类型信息，若带有非mscorlib带有的的数据类型需要在多应用程序间共享，需要在子类中使用
  Store.GetJson(string key)或者GetValue<T>(string key)方法。
  
  我的第一个可以使用的开源项目，我在我所负责的项目中已经开始使用，并不觉得很优雅，但是我认为在使用Session方面算是一种
  想法吧，希望有能看到的亲给予支持，提出意见。谢谢！
