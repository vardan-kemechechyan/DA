using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UI;

public class ReviewScreen : UIScreen
{
    [SerializeField] AppReview appReview;
    [SerializeField] GameObject[] ratingStars;

    int rating;

    public override void Open()
    {
        base.Open();

        rating = 5;

        UpdateRatingStars();
    }

    public void Rate(int rate)
    {
        rating = rate;
        UpdateRatingStars();
    }

    public void Rate() 
    {
        appReview.Rate(rating);
    }

    public void Later()
    {
        appReview.Rate(0);
    }

    private void UpdateRatingStars()
    {
        foreach (var star in ratingStars)
            star.transform.GetChild(1).gameObject.SetActive(false);

        for (int i = 0; i < rating; i++)
            ratingStars[i].transform.GetChild(1).gameObject.SetActive(true);
    }
}
