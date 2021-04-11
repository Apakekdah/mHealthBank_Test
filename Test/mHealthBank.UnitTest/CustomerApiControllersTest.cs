using mHealthBank.ApiController;
using mHealthBank.Business;
using mHealthBank.Entities;
using mHealthBank.Interfaces;
using mHealthBank.Models.Configuration;
using mHealthBank.Models.Forms;
using Microsoft.AspNetCore.Http;
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
        Mock<IUnitOfWorkAsync> mockUoW;
        DateTime now;
        Customers bllCustomer;

        CustomerController controller;

        Mock<IOptions<CustomerConfiguration>> mockCustCfg;

        public CustomerApiControllersTest()
        {
            now = DateTime.Now;
        }

        [TestInitialize]
        public void Setup()
        {
            repoIoC = new Mock<IDisposableIoC>();

            mockCustCfg = new Mock<IOptions<CustomerConfiguration>>();
            mockCustCfg.Setup(c => c.Value)
                .Returns(GetSettings<CustomerConfiguration>("CustomerConfiguration"));
            repoIoC.Setup(repo => repo.GetInstance<IOptions<CustomerConfiguration>>())
                .Returns(mockCustCfg.Object);

            var mockLogger = new Mock<ILogger>();
            repoIoC.Setup(repo => repo.GetInstance<ILogger>(It.IsAny<KeyValueParameter>()))
                .Returns(mockLogger.Object);

            var mockMapper = new Mock<IMappingObject>();
            repoIoC.Setup(repo => repo.GetInstance<IMappingObject>())
                .Returns(mockMapper.Object);

            var mockCustRepoAsync = new Mock<IRepositoryAsync<Customer>>();
            mockUoW = new Mock<IUnitOfWorkAsync>();

            repoIoC.SetupGet(c => c.New)
                .Returns(repoIoC.Object);

            bllCustomer = new Customers(mockCustRepoAsync.Object, mockUoW.Object, repoIoC.Object);

            repoBllCustomer = new Mock<Customers>(mockCustRepoAsync.Object, mockUoW.Object, repoIoC.Object);
            repoBllCustomer.Setup(bll => bll.GetAll())
                .ReturnsAsync(GetCustomers());
            repoBllCustomer.Setup(bll => bll.GetById(It.Is("1", StringComparer.OrdinalIgnoreCase)))
                .ReturnsAsync(GetCustomers().First(c => c.Id == "1"));
            //repoBllCustomer.Setup(bll => bll.Add(It.IsAny<Customer>()));

            repoIoC.Setup(repo => repo.GetInstance<Customers>())
                .Returns(bllCustomer);

            mockCustRepoAsync.Setup(bll => bll.GetById(It.Is("1", StringComparer.OrdinalIgnoreCase)))
                .ReturnsAsync(GetCustomers().First(c => c.Id == "1"));

            //mockCustRepoAsync.Setup(bll => bll.Add(It.IsAny<Customer>()));

            mockMapper.Setup(repo => repo.Get<Customer>(It.IsAny<CustomerModel>()))
                .Returns(GetCustomers().First());

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
            FileInfo fi = new FileInfo(Path.Combine(mockCustCfg.Object.Value.BasePath, "capturenux2012071901010.jpg"));

            return new Customer[]
            {
                new Customer
                {
                    Id = "1",
                    CustomerName = "Name 1",
                    KtpImage = "capturenux2012071901010.jpg",
                    KtpImageStream = fi.OpenRead(),
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

            Assert.IsInstanceOfType(jsonResult.Result, typeof(IEnumerable<Customer>));

            Assert.IsTrue((jsonResult.Result as IEnumerable<Customer>).Count() == 2);

            Assert.IsTrue(jsonResult.Success);
        }

        [TestMethod]
        public async Task GetTest()
        {
            var jsonResult = await controller.Get("1");

            Assert.IsInstanceOfType(jsonResult, typeof(JsonObject));

            Assert.IsInstanceOfType(jsonResult.Result, typeof(Customer));

            Assert.IsTrue((jsonResult.Result as Customer).CustomerName == "Name 1");
        }

        [TestMethod]
        public async Task GetImageTest()
        {
            var fileContent = await controller.GetImage("1");

            Assert.IsInstanceOfType(fileContent, typeof(FileStreamResult));
        }

        [TestMethod]
        public async Task AddTest()
        {
            mockUoW.Setup(bll => bll.Commit())
                .ReturnsAsync(1);

            IFormFile file = new MockFormFile(Path.Combine(mockCustCfg.Object.Value.BasePath, "capturenux2012071901010.jpg"));

            var jsonResult = await controller.Post(new Models.Forms.CustomerModel()
            {
                CustomerName = "Rudi",
                DateOfBirth = new DateTime(2000, 1, 1),
                File = file
            });

            Assert.IsInstanceOfType(jsonResult, typeof(JsonObject));

            Assert.IsTrue(jsonResult.Success);

            Assert.IsInstanceOfType(jsonResult.Result, typeof(int));

            Assert.IsTrue(((int)jsonResult.Result) == 1);
        }

        [TestMethod]
        public async Task AddToBusinessTest()
        {
            await bllCustomer.Add(GetCustomers().First());
        }
    }
}