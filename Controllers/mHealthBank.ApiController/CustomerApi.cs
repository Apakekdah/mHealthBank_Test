using mHealthBank.Business;
using mHealthBank.Entities;
using mHealthBank.Models.Forms;
using Microsoft.AspNetCore.Mvc;
using SAGE.Core.Commons;
using SAGE.IoC;
using System;
using System.Threading.Tasks;

namespace mHealthBank.ApiController
{
    /// <summary>
    ///  Simple Api Langsung ke Business Library / BLL
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerApiBase
    {
        public CustomerController(IDisposableIoC life)
            : base(life) { }

        [HttpGet("ktp/{id}")]
        public async Task<IActionResult> GetImage(string id)
        {
            using (var scope = life.New)
            {
                var bll = scope.GetInstance<Customers>();
                try
                {
                    Log.Info($"GetImage : WithId '{id}'");
                    var result = await bll.GetImage(id).ConfigureAwait(false);
                    return File(result.Stream, result.ContentType);
                }
                catch (Exception ex)
                {
                    Log.Error($"GetImage : WithId '{id}'", ex);
                    return NotFound(ex.Message);
                }
            }
        }

        [HttpGet("{id}")]
        public async Task<JsonObject> Get(string id)
        {
            using (var scope = life.New)
            {
                try
                {
                    Log.Info($"Get : WithId '{id}'");
                    var bll = scope.GetInstance<Customers>();
                    var result = await bll.GetById(id).ConfigureAwait(false);
                    if (result != null)
                        return new JsonObject(result);
                    return new JsonObject(false, $"Data with '{id}' not found");
                }
                catch (Exception ex)
                {
                    Log.Error($"Get : WithId '{id}'", ex);
                    return new JsonObject(false, ex.Message);
                }
            }
        }

        [HttpGet]
        public async Task<JsonObject> Get()
        {
            using (var scope = life.New)
            {
                try
                {
                    Log.Info("Get");
                    var bll = scope.GetInstance<Customers>();
                    var result = await bll.GetAll().ConfigureAwait(false);
                    return new JsonObject(result);
                }
                catch (Exception ex)
                {
                    Log.Error("Get", ex);
                    return new JsonObject(false, ex.Message);
                }
            }
        }

        [HttpPost]
        public async Task<JsonObject> Post([FromForm] CustomerModel customerModel)
        {
            using (var scope = life.New)
            {
                bool hasAdd = false;
                var bll = scope.GetInstance<Customers>();
                Customer customer = null;

                try
                {
                    Log.Info($"Post - AddNew '{customerModel.CustomerName}'");
                    Log.Trace($"Post - AddNew '{customerModel.CustomerName}'", new
                    {
                        CustomerName = customerModel.CustomerName,
                        BirtDate = customerModel.DateOfBirth,
                        KtpFile = customerModel?.File.FileName,
                        KtpFileSize = customerModel?.File.Length,
                        ContentType = customerModel?.File.ContentType,
                        ChangeBy = GetActiveUser()
                    });

                    customer = mapping.Get<Customer>(customerModel);
                    customer.CreateBy =
                        customer.UpdateBy = GetActiveUser();

                    await bll.Add(customer).ConfigureAwait(false);
                    hasAdd = true;
                    var result = await bll.Commit().ConfigureAwait(false);

                    Log.Info($"Post - AddNew '{customerModel.CustomerName}' - Success With Affected {result} Row(s)");

                    return new JsonObject(result);
                }
                catch (Exception ex)
                {
                    Log.Error($"Post - AddNew '{customerModel.CustomerName}'", ex);
                    if (hasAdd)
                    {
                        await bll.CleanUpFailed(customer.KtpImage).ConfigureAwait(false);
                    }
                    return new JsonObject(false, ex.Message);
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<JsonObject> Delete(string id)
        {
            using (var scope = life.New)
            {
                var bll = scope.GetInstance<Customers>();
                try
                {
                    Log.Info($"Delete : WithId '{id}'");
                    await bll.Delete(id).ConfigureAwait(false);
                    var result = await bll.Commit().ConfigureAwait(false);

                    return new JsonObject(result);
                }
                catch (Exception ex)
                {
                    Log.Error($"Delete : WithId '{id}'", ex);
                    return new JsonObject(false, ex.Message);
                }
            }
        }

        [HttpPut]
        public async Task<JsonObject> Put([FromForm] CustomerModel customerModel)
        {
            using (var scope = life.New)
            {
                if (customerModel.Id.IsNullOrEmpty())
                {
                    return new JsonObject(false, "Id not defined");
                }
                var bll = scope.GetInstance<Customers>();

                Customer customer = null;


                bool hasAdd = false;
                try
                {
                    Log.Error($"Info - Update '{customerModel.CustomerName}' WithId '{customerModel.Id}'");

                    customer = await bll.GetById(customerModel.Id);
                    if (customer == null)
                        throw new Exception("Customer not found");

                    customer.KtpImage = customerModel.File.FileName;
                    customer.KtpImageStream = customerModel.File.OpenReadStream();
                    customer.UpdateDt = DateTime.Now;
                    customer.UpdateBy = GetActiveUser();

                    Log.Trace($"Post - Update '{customerModel.CustomerName}'", new
                    {
                        Id = customerModel.Id,
                        CustomerName = customerModel.CustomerName,
                        BirtDate = customerModel.DateOfBirth,
                        KtpFile = customerModel?.File.FileName,
                        KtpFileSize = customerModel?.File.Length,
                        ContentType = customerModel?.File.ContentType,
                        ChangeBy = GetActiveUser()
                    });

                    Log.Error($"Info - Update '{customerModel.CustomerName}' WithId '{customerModel.Id}'");

                    await bll.Update(customer).ConfigureAwait(false);
                    hasAdd = true;
                    var result = await bll.Commit().ConfigureAwait(false);

                    return new JsonObject(result);
                }
                catch (Exception ex)
                {
                    Log.Error($"Put - Update '{customerModel.CustomerName}' WithId '{customerModel.Id}'", ex);
                    if (hasAdd)
                    {
                        await bll.CleanUpFailed(customer.KtpImage).ConfigureAwait(false);
                    }
                    return new JsonObject(false, ex.Message);
                }
            }
        }
    }
}
