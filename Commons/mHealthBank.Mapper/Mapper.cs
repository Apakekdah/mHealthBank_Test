using AutoMapper;
using mHealthBank.Entities;
using mHealthBank.Models.Forms;
using System;

namespace mHealthBank.Mapper
{
    public class Mapper : Profile
    {
        public Mapper()
        {
            CreateMap<CustomerModel, Customer>()
                .ForMember(c => c.Id, opt => opt.Ignore())
                .ForMember(c => c.CreateDt, opt => opt.MapFrom(c => DateTime.Now))
                .ForMember(c => c.UpdateDt, opt => opt.MapFrom(c => DateTime.Now))
                .ForMember(c => c.KtpImage, opt => opt.MapFrom(c => c.File != null ? c.File.FileName : null))
                .ForMember(c => c.KtpImageStream, opt => opt.MapFrom(c => c.File != null ? c.File.OpenReadStream() : null))
                ;
        }
    }
}