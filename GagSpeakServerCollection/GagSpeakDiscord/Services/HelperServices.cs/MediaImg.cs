using GagspeakDiscord.Modules.KinkDispenser;
using HtmlAgilityPack;
using System.Net;

namespace GagspeakDiscord.Services.HelperServices;

/// <summary>
/// Base class to hold the image list and the current image we are viewing
/// </summary>
public class MediaImg : PreviewImg
{
    /// <summary> The label of the image (title) </summary>
    public string Label { get; set; }

    /// <summary> The direct url to the fullres image </summary>
    public string FullResURL { get; set; }

    /// <summary> The referer uri to the fullres image page </summary>
    public Uri FullresReferer { get; set; }

    /// <summary> The tags associated with the image </summary>
    public List<string> Tags { get; set; } = new List<string>();

    /// <summary> The width and height of the fullresImage image </summary>
    public (ushort Width, ushort Height) FullResImgSize { get; set; }

    /// <summary> 
    /// Takes the fullResReferer and fetches the fullresURL link from it
    /// <para> This is done by making a request to the fullResReferer and parsing the html for the fullresURL </para>
    /// </summary>
    public async Task FetchFullResURL(ILogger<KinkDispenser> logger, SelectionDataService dataService)
    {
        logger.LogInformation("Fetching full resolution page: {fullResUrl}", this.FullresReferer);
        try
        {
            // Fetch the full resolution page
            using var fullResPageResponse = await dataService.Img_HttpClient.GetAsync(this.FullresReferer).ConfigureAwait(false);

            // if the request was successful, parse the html for the full resolution image URL
            if (fullResPageResponse.IsSuccessStatusCode)
            {
                var fullResPageHtml = await fullResPageResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var fullResPageHtmlDoc = new HtmlDocument();
                // load the html
                fullResPageHtmlDoc.LoadHtml(fullResPageHtml);
                // Assuming the full resolution image URL is in an img tag with class 'full_res_image'
                var fullResUrlNode = fullResPageHtmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'big_pin_box')]//div[@class='image_frame']//img");
                var encodedUrl = fullResUrlNode.GetAttributeValue("src", "");
                // set the full resolution URL
                this.FullResURL = WebUtility.HtmlDecode(encodedUrl);

                // Get the width and height attributes
                ushort widthAttribute = ushort.TryParse(fullResUrlNode.GetAttributeValue("width", ""), out var width) ? width : (ushort)0;
                ushort heightAttribute = ushort.TryParse(fullResUrlNode.GetAttributeValue("height", ""), out var height) ? height : (ushort)0;
                // Parse the width and height values
                this.FullResImgSize = (widthAttribute, heightAttribute);

                // Set the FullResImgSize
                this.FullResImgSize = (width, height);

                logger.LogInformation("Found full resolution image: {tmpMediaImg.FullResURL}", this.FullResURL);
            }
            else
            {
                logger.LogWarning("Failed to access full resolution page: {fullResUrl} with status code {statusCode}", this.FullResURL, fullResPageResponse.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error fetching full resolution page: {fullResUrl}", this.FullResURL);
        }
    }

    /// <summary>
    /// Im still braindead dumb and dont know how to merge functions like these yet so this is the same thing as above except with boardservice
    /// </summary>
    public async Task FetchFullResURL(ILogger<KinkDispenser> logger, SelectionBoardService dataService)
    {
        logger.LogInformation("Fetching full resolution page: {fullResUrl}", this.FullresReferer);
        try
        {
            // Fetch the full resolution page
            using var fullResPageResponse = await dataService.Board_HttpClient.GetAsync(this.FullresReferer).ConfigureAwait(false);

            // if the request was successful, parse the html for the full resolution image URL
            if (fullResPageResponse.IsSuccessStatusCode)
            {
                var fullResPageHtml = await fullResPageResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var fullResPageHtmlDoc = new HtmlDocument();
                // load the html
                fullResPageHtmlDoc.LoadHtml(fullResPageHtml);
                // Assuming the full resolution image URL is in an img tag with class 'full_res_image'
                var fullResUrlNode = fullResPageHtmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'big_pin_box')]//div[@class='image_frame']//img");
                var encodedUrl = fullResUrlNode.GetAttributeValue("src", "");
                // set the full resolution URL
                this.FullResURL = WebUtility.HtmlDecode(encodedUrl);

                // Get the width and height attributes
                var widthAttribute = fullResUrlNode.GetAttributeValue("width", "");
                var heightAttribute = fullResUrlNode.GetAttributeValue("height", "");
                // Parse the width and height values
                ushort width = 0;
                ushort height = 0;
                ushort.TryParse(widthAttribute, out width);
                ushort.TryParse(heightAttribute, out height);
                // Set the FullResImgSize
                this.FullResImgSize = (width, height);

                logger.LogInformation("Found full resolution image: {tmpMediaImg.FullResURL}", this.FullResURL);
            }
            else
            {
                logger.LogWarning("Failed to access full resolution page: {fullResUrl} with status code {statusCode}", this.FullResURL, fullResPageResponse.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error fetching full resolution page: {fullResUrl}", this.FullResURL);
        }
    }
}
