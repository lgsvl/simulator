#include <iostream>
#include <opencv2/opencv.hpp>
#include <opencv2/ximgproc.hpp>

int main(int argc, char** argv )
{
    std::string inputFile = argv[1];

    cv::Mat img = cv::imread(inputFile, cv::IMREAD_GRAYSCALE);

    cv::Mat small;
    cv::resize(img, small, cv::Size(0,0), 0.25, 0.25, cv::INTER_AREA);

    cv::Mat bordered;
    cv::copyMakeBorder(small, bordered, 16, 16, 16, 16, cv::BORDER_CONSTANT, cv::Scalar(0, 0, 0));

    cv::Ptr<cv::ximgproc::FastLineDetector> fld = cv::ximgproc::createFastLineDetector();
    std::vector<cv::Vec4f> lines;
    fld->detect(bordered, lines);

    for (size_t i = 0; i < lines.size(); ++i)
    {
        cv::Vec4f line = lines[i];
        for (size_t j = 0; j < 4; ++j)
            line.val[j] = (line.val[j] - 16) / small.cols;
        std::cout << line << std::endl;
    }
}
