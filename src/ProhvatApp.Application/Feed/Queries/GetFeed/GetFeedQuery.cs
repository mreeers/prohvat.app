using System;
using System.Collections.Generic;
using MediatR;
using ProhvatApp.Application.Feed.DTOs;
using ProhvatApp.Domain.Enums;

namespace ProhvatApp.Application.Feed.Queries.GetFeed;

public record GetFeedQuery(
    bool OnlyFriends = false,
    SeasonType? Season = null,
    Guid? CurrentUserId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<List<FeedItemDto>>;
