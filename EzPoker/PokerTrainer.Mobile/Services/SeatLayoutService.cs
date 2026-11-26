using PokerTrainer.Mobile.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Graphics;

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

        public Point GetSeatCenter(int index, int count, double width, double height, double seatWidth, double seatHeight)
        {
            const double margin = 6;

            // Giới hạn đường đi của tâm ghế để cả thẻ nằm trong layout.
            double left = margin + seatWidth / 2;
            double right = width - margin - seatWidth / 2;
            double top = margin + seatHeight / 2;
            double bottom = height - margin - seatHeight / 2;

            double cx = (left + right) / 2;
            double cy = (top + bottom) / 2;
            double radius = Math.Min(right - left, bottom - top) / 2;

            if (radius <= 0)
                return new Point(cx, cy);

            // Bàn ngang có hai đoạn thẳng ngang.
            // Bàn dọc dùng cùng phép tính, sau đó xoay tọa độ.
            bool vertical = bottom - top > right - left;
            double longSide = vertical ? bottom - top : right - left;
            double halfStraight = longSide / 2 - radius;
            double halfArc = Math.PI * radius;
            double perimeter = 4 * halfStraight + 2 * halfArc;
            double d = perimeter * index / count;

            Point p;

            if (d < halfStraight)
            {
                p = new Point(-d, radius);
            }
            else if ((d -= halfStraight) < halfArc)
            {
                double angle = Math.PI / 2 + d / radius;
                p = new Point(
                    -halfStraight + radius * Math.Cos(angle),
                    radius * Math.Sin(angle));
            }
            else if ((d -= halfArc) < 2 * halfStraight)
            {
                p = new Point(-halfStraight + d, -radius);
            }
            else if ((d -= 2 * halfStraight) < halfArc)
            {
                double angle = -Math.PI / 2 + d / radius;
                p = new Point(
                    halfStraight + radius * Math.Cos(angle),
                    radius * Math.Sin(angle));
            }
            else
            {
                p = new Point(halfStraight - (d - halfArc), radius);
            }

            if (!vertical)
                return new Point(cx + p.X, cy + p.Y);

            // Xoay sang bàn dọc, bắt đầu từ điểm dưới cùng của cung tròn.
            double shifted = (perimeter * index / count + halfStraight + halfArc / 2)
                % perimeter;
            return GetVerticalSeatCenter(shifted, halfStraight, radius, cx, cy);
        }

        private static Point GetVerticalSeatCenter(
            double d, double halfStraight, double r, double cx, double cy)
        {
            double quarterArc = Math.PI * r / 2;
            double halfArc = 2 * quarterArc;
            double straight = 2 * halfStraight;

            // d được đổi lại thành khoảng cách tính từ đáy bàn dọc.
            d -= halfStraight + quarterArc;
            if (d < 0)
                d += 2 * straight + 2 * halfArc;

            if (d < quarterArc)
                return Arc(halfStraight, Math.PI / 2 + d / r);

            d -= quarterArc;
            if (d < straight)
                return new Point(cx - r, cy + halfStraight - d);

            d -= straight;
            if (d < halfArc)
                return Arc(-halfStraight, Math.PI + d / r);

            d -= halfArc;
            if (d < straight)
                return new Point(cx + r, cy - halfStraight + d);

            return Arc(halfStraight, (d - straight) / r);

            Point Arc(double centerY, double angle) => new(
                cx + r * Math.Cos(angle),
                cy + centerY + r * Math.Sin(angle));
        }
    }
}
