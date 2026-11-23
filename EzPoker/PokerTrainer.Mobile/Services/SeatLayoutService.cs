using PokerTrainer.Mobile.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace PokerTrainer.Mobile.Services
{
    public class SeatLayoutService
    {
        public void ArrangeSeats(IReadOnlyList<PlayerSeatViewModel> seats)
        {
            if(seats.Count < 2)
            {
                throw new ArgumentException("Bàn phải có ít nhất 2 người chơi.");
            }

            if(seats.Count > 10)
            {
                throw new ArgumentException("Bàn không thể có nhiều hơn 10 người chơi.");
            }

            double centerX = 0.5; // Tâm của bàn (theo tỷ lệ phần trăm)
            double centerY = 0.5; // Tâm của bàn (theo tỷ lệ phần trăm)

            //Ban kinh theo chieu ngang va doc cua ban Oval
            double radiusX = 0.44; // Bán kính theo chiều ngang (theo tỷ lệ phần trăm)
            double radiusY = 0.42; // Bán kính theo chiều dọc (theo tỷ lệ phần trăm)

            for(int index = 0; index < seats.Count; index++)
            {
                /*
                * Ghế đầu tiên nằm ở phía dưới bàn.
                * Các ghế tiếp theo chạy theo chiều kim đồng hồ.
                */
                double angle = Math.PI / 2 + 2 * Math.PI * index / seats.Count; // Bắt đầu từ phía dưới bàn (90 độ)

                seats[index].X = centerX + radiusX * Math.Cos(angle);

                seats[index].Y = centerY + radiusY * Math.Sin(angle);
            }
        }
    }
}
