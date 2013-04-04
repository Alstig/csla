﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !NUNIT && !ANDROID
using Microsoft.VisualStudio.TestTools.UnitTesting;
#elif !ANDROID
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#endif 

namespace Csla.Test.DataPortal
{
  [TestClass]
  public class InterceptorTests
  {
    [TestInitialize]
    public void Setup()
    {
      Csla.Server.DataPortal.InterceptorType = typeof(TestInterceptor);
      Csla.ApplicationContext.DataPortalActivator = new TestActivator();
    }

    [TestCleanup]
    public void Cleanup()
    {
      Csla.Server.DataPortal.InterceptorType = null;
      Csla.ApplicationContext.DataPortalActivator = null;
    }

    [TestMethod]
    public void FetchWithIntercept()
    {
      Csla.ApplicationContext.GlobalContext.Clear();

      var obj = Csla.DataPortal.Fetch<InitializeRoot>("abc");
      Assert.AreEqual("Initialize", Csla.ApplicationContext.GlobalContext["Intercept1+InitializeRoot"].ToString(), "Initialize should have run");
      Assert.AreEqual("Complete", Csla.ApplicationContext.GlobalContext["Intercept2+InitializeRoot"].ToString(), "Complete should have run");
      Assert.AreEqual("CreateInstance", Csla.ApplicationContext.GlobalContext["Activate1+InitializeRoot"].ToString(), "CreateInstance should have run");
      Assert.AreEqual("InitializeInstance", Csla.ApplicationContext.GlobalContext["Activate2+InitializeRoot"].ToString(), "InitializeInstance should have run");
    }

    [TestMethod]
    public void FetchListWithIntercept()
    {
      Csla.ApplicationContext.GlobalContext.Clear();

      var obj = Csla.DataPortal.Fetch<InitializeListRoot>();
      Assert.AreEqual("Initialize", Csla.ApplicationContext.GlobalContext["Intercept1+InitializeListRoot"].ToString(), "Initialize should have run");
      Assert.AreEqual("Complete", Csla.ApplicationContext.GlobalContext["Intercept2+InitializeListRoot"].ToString(), "Complete should have run");
      Assert.AreEqual("CreateInstance", Csla.ApplicationContext.GlobalContext["Activate1+InitializeListRoot"].ToString(), "CreateInstance (list) should have run");
      Assert.AreEqual("InitializeInstance", Csla.ApplicationContext.GlobalContext["Activate2+InitializeListRoot"].ToString(), "InitializeInstance (list) should have run");

      Assert.AreEqual("CreateInstance", Csla.ApplicationContext.GlobalContext["Activate1+InitializeRoot"].ToString(), "CreateInstance should have run");
      Assert.AreEqual("InitializeInstance", Csla.ApplicationContext.GlobalContext["Activate2+InitializeRoot"].ToString(), "InitializeInstance should have run");
    }

    [TestMethod]
    public void InterceptException()
    {
      Csla.ApplicationContext.GlobalContext.Clear();

      try
      {
        var obj = Csla.DataPortal.Fetch<InitializeRoot>("boom");
      }
      catch
      { }
      Assert.AreEqual("Initialize", Csla.ApplicationContext.GlobalContext["Intercept1+InitializeRoot"].ToString(), "Initialize should have run");
      Assert.AreEqual("Complete", Csla.ApplicationContext.GlobalContext["Intercept2+InitializeRoot"].ToString(), "Complete should have run");
      Assert.IsTrue(!string.IsNullOrEmpty(Csla.ApplicationContext.GlobalContext["InterceptException+InitializeRoot"].ToString()), "Complete should have exception");
    }

    [TestMethod]
    public void UpdateWithIntercept()
    {
      Csla.ApplicationContext.GlobalContext.Clear();

      var obj = Csla.DataPortal.Fetch<InitializeRoot>("abc");
      Csla.ApplicationContext.GlobalContext.Clear();

      obj.Name = "xyz";
      obj = obj.Save();

      Assert.AreEqual("Initialize", Csla.ApplicationContext.GlobalContext["Intercept1+InitializeRoot"].ToString(), "Initialize should have run");
      Assert.AreEqual("Complete", Csla.ApplicationContext.GlobalContext["Intercept2+InitializeRoot"].ToString(), "Complete should have run");
      Assert.IsFalse(Csla.ApplicationContext.GlobalContext.Contains("Activate1+InitializeRoot"), "CreateInstance should not have run");
      Assert.AreEqual("InitializeInstance", Csla.ApplicationContext.GlobalContext["Activate2+InitializeRoot"].ToString(), "InitializeInstance should have run");
    }

    [TestMethod]
    public void UpdateListWithIntercept()
    {
      Csla.ApplicationContext.GlobalContext.Clear();

      var obj = Csla.DataPortal.Fetch<InitializeListRoot>();
      Csla.ApplicationContext.GlobalContext.Clear();

      obj[0].Name = "xyz";
      obj = obj.Save();

      Assert.AreEqual("Initialize", Csla.ApplicationContext.GlobalContext["Intercept1+InitializeListRoot"].ToString(), "Initialize should have run");
      Assert.AreEqual("Complete", Csla.ApplicationContext.GlobalContext["Intercept2+InitializeListRoot"].ToString(), "Complete should have run");
      Assert.IsFalse(Csla.ApplicationContext.GlobalContext.Contains("Activate1+InitializeListRoot"), "CreateInstance (list) should not have run");
      Assert.AreEqual("InitializeInstance", Csla.ApplicationContext.GlobalContext["Activate2+InitializeListRoot"].ToString(), "InitializeInstance (list) should have run");

      Assert.IsFalse(Csla.ApplicationContext.GlobalContext.Contains("Activate1+InitializeRoot"), "CreateInstance should not have run");
      Assert.AreEqual("InitializeInstance", Csla.ApplicationContext.GlobalContext["Activate2+InitializeRoot"].ToString(), "InitializeInstance should have run");
    }
  }

  [Serializable]
  public class InitializeRoot : BusinessBase<InitializeRoot>
  {
    public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(c => c.Name);
    public string Name
    {
      get { return GetProperty(NameProperty); }
      set { SetProperty(NameProperty, value); }
    }

    private void DataPortal_Fetch(string name)
    {
      Fetch(name);
    }

    private void Child_Fetch(string name)
    {
      Fetch(name);
    }

    private void Fetch(string name)
    {
      if (name == "boom")
        throw new Exception("boom");

      using (BypassPropertyChecks)
      {
        Name = name;
      }
    }

    protected override void DataPortal_Update()
    {
    }

    private void Child_Update()
    {
    }
  }

  [Serializable]
  public class InitializeListRoot : BusinessListBase<InitializeListRoot, InitializeRoot>
  {
    private void DataPortal_Fetch()
    {
      using (SuppressListChangedEvents)
      {
        Add(Csla.DataPortal.FetchChild<InitializeRoot>("abc"));
      }
    }

    protected override void DataPortal_Update()
    {
      base.Child_Update();
    }
  }

  public class TestInterceptor : Csla.Server.IInterceptDataPortal
  {
    public void Initialize(Server.InterceptArgs e)
    {
      Csla.ApplicationContext.GlobalContext["Intercept1+" + e.ObjectType.Name] = "Initialize";
    }

    public void Complete(Server.InterceptArgs e)
    {
      Csla.ApplicationContext.GlobalContext["Intercept2+" + e.ObjectType.Name] = "Complete";
      if (e.Exception != null)
        Csla.ApplicationContext.GlobalContext["InterceptException+" + e.ObjectType.Name] = e.Exception.Message;
    }
  }

  public class TestActivator : Csla.Server.IDataPortalActivator
  {
    public object CreateInstance(Type requestedType)
    {
      Csla.ApplicationContext.GlobalContext["Activate1+" + requestedType.Name] = "CreateInstance";
      return Activator.CreateInstance(requestedType);
    }

    public void InitializeInstance(object obj)
    {
      Csla.ApplicationContext.GlobalContext["Activate2+" + obj.GetType().Name] = "InitializeInstance";
    }
  }
}