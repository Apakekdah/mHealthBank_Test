using mHealthBank.ApiController;
using mHealthBank.Business;
using mHealthBank.Entities;
using mHealthBank.Interfaces;
using mHealthBank.Models.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAGE.Core.Interface;
using SAGE.IoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace mHealthBank.UnitTest
{
    [TestClass]
    public class CustomerApiControllersTest
    {
        Mock<IDisposableIoC> repoIoC;
        Mock<Customers> repoBllCustomer;
        DateTime now;

        CustomerController controller;

        public CustomerApiControllersTest()
        {
            now = DateTime.Now;
        }

        [TestInitialize]
        public void Setup()
        {
            repoIoC = new Mock<IDisposableIoC>();

            var mockMapper = new Mock<IMappingObject>();

            var mockCustRepoAsync = new Mock<IRepositoryAsync<Customer>>();
            var mockUoW = new Mock<IUnitOfWorkAsync>();

            var mockLogger = new Mock<ILogger>();
            var mockCustCfg = new Mock<IOptions<CustomerConfiguration>>();
            mockCustCfg.Setup(c => c.Value)
                .Returns(GetSettings<CustomerConfiguration>("CustomerConfiguration"));

            repoIoC.SetupGet(c => c.New)
                .Returns(repoIoC.Object);

            repoBllCustomer = new Mock<Customers>(mockCustRepoAsync.Object, mockUoW.Object, repoIoC.Object);
            repoBllCustomer.Setup(bll => bll.GetAll())
                .ReturnsAsync(GetCustomers());

            repoIoC.Setup(repo => repo.GetInstance<ILogger>(It.IsAny<KeyValueParameter>()))
                .Returns(mockLogger.Object);

            repoIoC.Setup(repo => repo.GetInstance<IOptions<CustomerConfiguration>>())
                .Returns(mockCustCfg.Object);

            repoIoC.Setup(repo => repo.GetInstance<Customers>())
                .Returns(repoBllCustomer.Object);

            repoIoC.Setup(repo => repo.GetInstance<IMappingObject>())
                .Returns(mockMapper.Object);

            controller = new CustomerController(repoIoC.Object);
        }

        public T GetSettings<T>(string property)
            where T : class, new()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var section = builder.GetSection(property);

            var instance = new T();
            if (!section.Exists())
                return instance;

            section.Bind(instance, opt => opt.BindNonPublicProperties = false);

            return instance;
        }

        public IEnumerable<Customer> GetCustomers()
        {
            return new Customer[]
            {
                new Customer
                {
                    Id = "1",
                    CustomerName = "Name 1",
                    KtpImage = "Ktp.jpg",
                    CreateBy = "test",
                    CreateDt = now,
                    UpdateBy = "test",
                    UpdateDt = now
                },
                new Customer
                {
                    Id = "2",
                    CustomerName = "Name 2",
                    KtpImage = "Ktp.jpg",
                    CreateBy = "test",
                    CreateDt = now,
                    UpdateBy = "test",
                    UpdateDt = now
                }
            };
        }

        [TestMethod]
        public async Task GetAllTest()
        {
            var jsonResult = await controller.Get();

            Assert.IsInstanceOfType(jsonResult, typeof(JsonObject));

            Assert.IsTrue((jsonResult.Result as IEnumerable<Customer>).Count() == 2);

            Assert.IsTrue(jsonResult.Success);
        }
    }
}