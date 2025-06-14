namespace Nemui.Shared.Enums;

public enum QuestionType
{
    Unspecified = 0,       // Sentinel value - indicates database default should be used
    MultipleChoice = 1,    // Chọn nhiều đáp án
    TrueFalse = 2,         // Đúng/Sai
    FillInTheBlank = 3,    // Điền vào chỗ trống
    Matching = 4,          // Ghép cặp
    Ordering = 5,          // Sắp xếp thứ tự
}