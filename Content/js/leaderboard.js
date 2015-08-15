//------------------------------------------------------------------------------
// Page Settings
//------------------------------------------------------------------------------
var kPaginationSize = 10;

//------------------------------------------------------------------------------
// Initialize
//------------------------------------------------------------------------------
$(function () {
    InitializeRankingData();
    InitializePagination();

    if (loggedInUserId == 0)
    {
        $("#row-view-my-rank").hide();
    }
    else
    {
        $("#view-my-rank-a").attr("href", leaderboardPathPrefix + 'my');
    }
});

function InitializeRankingData() {
    for (currentIndex in pageData) {
        var currentRankData = pageData[currentIndex];
        $('#rank-table tbody').append('<tr><th scope="row">' + currentRankData.rank + '</th><td><a href="' + userProfilePathPrefix + currentRankData.userId + '">' +currentRankData.userName + '</a></td><td>' +currentRankData.point + '</td></tr>');
    }
}

function InitializePagination() {
    if (currentPage == 1) {
    $('#pagination-prev-first').addClass('disabled');
    $('#pager-prev').addClass('disabled');
    }
$("#pagination-prev-first-a").attr("href", leaderboardPathPrefix + '1');
$("#pager-prev-a").attr("href", leaderboardPathPrefix +(currentPage -1));

    var prevPageSizeOffsetted = currentPage - kPaginationSize;
    if (prevPageSizeOffsetted < 1) {
        $('#pagination-prev-page').addClass('disabled');
        }
        else {
        $('#pagination-prev-page-index').text(prevPageSizeOffsetted + ' ');
        $("#pagination-prev-page-a").attr("href", leaderboardPathPrefix +prevPageSizeOffsetted);
        }

    if(currentPage == maxPage) {
    $('#pagination-next-last').addClass('disabled');
    $('#pager-next').addClass('disabled');
}
$('#pagination-next-last-index').text(maxPage);
$("#pagination-next-last-a").attr("href", leaderboardPathPrefix +maxPage);
$("#pager-next-a").attr("href", leaderboardPathPrefix +(currentPage +1));

    var nextPageSizeOffsetted = currentPage +kPaginationSize;
    if (nextPageSizeOffsetted > maxPage) {
        $('#pagination-next-page').addClass('disabled');
        }
        else {
        $('#pagination-next-page-index').text(' ' +nextPageSizeOffsetted);
        $("#pagination-next-page-a").attr("href", leaderboardPathPrefix +nextPageSizeOffsetted);
        }

    var firstPage = Math.floor((currentPage - 1) / kPaginationSize) * kPaginationSize +1;

    for (i = firstPage +kPaginationSize -1; i >= firstPage; i--) {
        if (i == currentPage)
        {
            $("#pagination-prev-page").after('<li class="active"><a href="' +leaderboardPathPrefix +i + '">' +i + '</a></li>');
            }
            else
            {
            $("#pagination-prev-page").after('<li><a href="' +leaderboardPathPrefix + i + '">' + i + '</a></li>');
    }
}
}