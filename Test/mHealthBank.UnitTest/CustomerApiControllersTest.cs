using mHealthBank.ApiController;
using mHealthBank.Business;
using mHealthBank.Entities;
using mHealthBank.Interfaces;
using mHealthBank.Models.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SAGE.Core.Interface;
using SAGE.IoC;
using System;
using System.Collections.Generic;
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
            
            var mockCustCfg = new Mock<IOptions<CustomerConfiguration>>();
            mockCustCfg.Setup(c=> c.Value)
                .Returns(new CustomerConfiguration())
            repoBllCustomer = new Mock<Customers>(mockCustRepoAsync.Object, mockUoW.Object, repoIoC.Object);
            repoBllCustomer.Setup(bll => bll.GetAll())
                .ReturnsAsync(GetCustomers());

            repoIoC.Setup(repo => repo.GetInstance<Customers>())
                .Returns(repoBllCustomer.Object);

            repoIoC.Setup(repo => repo.GetInstance<IMappingObject>())
                .Returns(mockMapper.Object);

            controller = new CustomerController(repoIoC.Object);
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
        }
    }
}