# TextImageGenerator
This purpose of this project is to gather printed word/char images as training data for machine learning task. 
* To gather word level data, google open book is scanned, then the original data can be synthetized. These three projects need to be run in order: **BookDownloader** - **BookImageProcessor** - **ImageSynthesizer**

* To gather single char level data, windows/google font images containing single char are generated, then the original data can be synthetized. These two projects need to be run in order: **CharGenerator** - **ImageSynthesizer**

currently ImageSynthesizer only synthesizes single char level data, but the code base can be modified easiliy to work on word level images. I am not sure about what what best pratice is for image synthesization here.  

## BookDownloader
Use selenium to navigate through google open books and download each book page as one image and save to harddisk. 

### Configuration

* Configure App.config - appsetting - BookDataPath, so the program knows where to save the images in the harddrive
* Configure Book.cs, so the program knows which books to navigate



## BookImageProcessor
extract word images from the images extracted from **BookDownloader** using OCR Service - Oxford

### Configuration

* Configure App.config - appsetting - BookDataPath, the path should point to the folder where the images are downloaded from 
**BookDownloader** 
* Configure App.config - appsetting - OxfordKey



## CharGenerator
extract single character images from various windows and google fonts. Windows fonts come with operating system, google fonts can be downloaded here, https://github.com/google/fonts. This project can be used to generate word level images as well with trival changes.
some fonts can generate unnormal styled characters, we perform manual control and remove those and generate a file "fonts_large.txt"
containing a list of normal font names. As some font generates a lot of offset at the top and bottom, we remove those fonts and generate
another file "fonts_small.txt". When image are generated, it will only generate images for the font names that can be found from "fonts_small.txt"

### Configuration

* App.config - appsetting - ExportDir, Location there char images will be exported to 
* App.config - appsetting - GeneratedChars, a complete string containing all chars that should be exported as images
* App.config - appsetting - GoogleFontDir, after downloaded the complete google font project from git, the "ofl" folder
* App.config - appsetting - DefaultExportFontSize, the default font size used to generate image



## ImageSynthesizer

apply Gaussian blurring and auto contrast according to this tutorial for EMGUCV 3.0, to generate sythetic images, the goal is to raise 1900 trainning images for each charater to 5000-6000

http://docs.opencv.org/trunk/dc/dd3/tutorial_gausian_median_blur_bilateral_filter.html

* App.config - appsetting - FontDataDir, location for images containing char/word images generated from "CharGenerator"
* App.config - appsetting - FontDataDirDest, location for synthesized images 









