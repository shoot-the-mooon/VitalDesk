using System;

namespace VitalDesk.App.Extensions;

public static class DateTimeExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
    {
        // 有効な範囲をチェック（year 1-9999）
        if (dateTime.Year < 1 || dateTime.Year > 9999)
        {
            return DateTimeOffset.Now; // デフォルト値として現在時刻を返す
        }
        
        try
        {
            return new DateTimeOffset(dateTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTimeOffset.Now; // 変換失敗時のフォールバック
        }
    }
    
    public static DateTimeOffset? ToDateTimeOffset(this DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;
            
        // 有効な範囲をチェック（year 1-9999）
        if (dateTime.Value.Year < 1 || dateTime.Value.Year > 9999)
        {
            return null; // 無効な値の場合はnullを返す
        }
        
        try
        {
            return new DateTimeOffset(dateTime.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null; // 変換失敗時はnullを返す
        }
    }
} 