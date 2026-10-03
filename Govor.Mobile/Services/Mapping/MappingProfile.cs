using AutoMapper;
using Govor.Mobile.Data;
using Govor.Mobile.Models.Responses;
using Govor.Mobile.PageModels.ContentViewsModel;
using Govor.Mobile.PageModels.ContentViewsModel.Messages;
using Govor.Mobile.Models;

namespace Govor.Mobile.Services.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<MessageViewResponse, MessageView>();
        CreateMap<MessageView, MessageViewResponse>();
        CreateMap<MediaAttachmentResponse, MediaFile>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.MediaFileId));
        CreateMap<MediaFile, MediaAttachmentResponse>()
            .ForMember(d => d.MediaFileId, o => o.MapFrom(s => s.Id));
        CreateMap<MessageResponse, LocalMessage>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.LocalStatus, opt => opt.Ignore())
            .ForMember(dest => dest.ChatId, opt => opt.MapFrom(src => src.RecipientId));
            
        // 2. Из события SignalR в локальную сущность
        // (Обычно в SignalR DTO поля называются MessageId вместо Id)
        CreateMap<UserMessageResponse, LocalMessage>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.MessageId))
            .ForMember(dest => dest.LocalStatus, opt => opt.Ignore())
            .ForMember(dest => dest.ChatId, opt => opt.MapFrom(src => src.RecipientId));
        
        // 3. Обратный маппинг для UI
        // Когда ViewModel запрашивает данные из репозитория, она должна получать MessageResponse
        CreateMap<LocalMessage, MessageResponse>()
            .ForMember(dest => dest.RecipientId, opt => opt.MapFrom(src => src.ChatId));
        
        // 4. Маппинг для обновления существующих записей (используется в репозитории)
        
        // Маппинг сообщения
        CreateMap<MessageResponse, MessagesViewModel>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.EncryptedContent))
            .ForMember(dest => dest.SenderId, opt => opt.MapFrom(srs => srs.SenderId))
            // Игнорируем поля, которые заполнит MappingAction
            .ForMember(dest => dest.Time, opt => opt.Ignore())
            .ForMember(dest => dest.IsIncoming, opt => opt.Ignore())
            //.ForMember(dest => dest.Attachments, opt => opt.Ignore())
            .ForMember(dest => dest.Avatar, opt => opt.Ignore())
            // Подключаем наш Action
            .AfterMap<MessageResponseToViewModelAction>();
    }
}
