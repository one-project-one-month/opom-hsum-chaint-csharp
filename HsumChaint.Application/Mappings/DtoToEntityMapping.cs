using AutoMapper;
using HsumChaint.Application.DTOs.Notification;
using HsumChaint.Application.DTOs.User;
using HsumChaint.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace HsumChaint.Application.Mappings
{
    public class DtoToEntityMappingProfile : Profile
    {
        public DtoToEntityMappingProfile()
        {
            CreateMap<UserDto, User>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ReverseMap();

            CreateMap<CreateNotificationRequestDto, Notification>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.NotificationType))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.IsDelete, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now));

            CreateMap<Notification, CreateNotificationResponseDto>()
                .ForMember(dest => dest.NotificationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.NotificationType, opt => opt.MapFrom(src => src.Type));

            CreateMap<Notification, ReadNotificationResponseDto>()
                .ForMember(dest => dest.NotificationId, opt => opt.MapFrom(src => src.Id));

            CreateMap<Notification, DeleteNotificationResponseDto>()
                .ForMember(dest => dest.NotificationId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDelete ?? false));

            CreateMap<Invitation, InvitationDto>();
            CreateMap<InvitationDto, Invitation>();

            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationDto, Notification>();

            //CreateMap<UserDto, User>()
            //    .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
