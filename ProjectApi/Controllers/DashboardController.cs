import React, { useEffect, useState } from "react";
import axios from "axios";
import { Line } from "react-chartjs-2";
import {
  Chart as ChartJS,
  LineElement,
  CategoryScale,
  LinearScale,
  PointElement,
  Tooltip,
  Legend,
} from "chart.js";

ChartJS.register(LineElement, CategoryScale, LinearScale, PointElement, Tooltip, Legend);

const API_BASE_URL = process.env.REACT_APP_API_URL || "http://localhost:5186";

export default function AdminPage() {
  const [stats, setStats] = useState(null);

  const [range, setRange] = useState("week");
  const [revenueChart, setRevenueChart] = useState(null);
  const [visitorChart, setVisitorChart] = useState(null);

  const [dropdownOpen, setDropdownOpen] = useState(false);

  // =============================
  // LOAD OVERVIEW
  // =============================
  useEffect(() => {
    axios
      .get(`${API_BASE_URL}/api/dashboard/overview`)
      .then((res) => setStats(res.data))
      .catch((err) => console.error("Overview error:", err));
  }, []);

  // =============================
  // LOAD CHART WHEN RANGE CHANGES
  // =============================
  useEffect(() => {
    axios
      .get(`${API_BASE_URL}/api/dashboard/revenue-chart?range=${range}`)
      .then((res) => {
        setRevenueChart({
          labels: res.data.map((x) => x.date),
          datasets: [
            {
              label: "Doanh thu (VND)",
              data: res.data.map((x) => x.revenue),
              borderColor: "#28a745",
              fill: false,
              tension: 0.3,
            },
          ],
        });
      });

    axios
      .get(`${API_BASE_URL}/api/dashboard/visit-chart?range=${range}`)
      .then((res) => {
        setVisitorChart({
          labels: res.data.map((x) => x.date),
          datasets: [
            {
              label: "Lượt truy cập",
              data: res.data.map((x) => x.count),
              borderColor: "#007bff",
              fill: false,
              tension: 0.3,
            },
          ],
        });
      });
  }, [range]);

  if (!stats) return <div className="text-center mt-5">Đang tải Dashboard...</div>;

  // =============================
  // DROPDOWN LABEL
  // =============================
  const labelMap = {
    week: "Tuần",
    month: "Tháng",
    year: "Năm",
    all: "Tất cả",
  };

  return (
    <>
      {/* HEADER */}
      <div className="row">
        <div className="col-md-12 page-header">
          <div className="page-pretitle">Overview</div>
          <h2 className="page-title">Dashboard</h2>
        </div>
      </div>

      {/* ------------------------------- */}
      {/* 📌 COMPONENT RANGE — DROPDOWN FB */}
      {/* ------------------------------- */}
      <div className="fb-dropdown mt-3">
        <button
          className="fb-dropbtn"
          onClick={() => setDropdownOpen(!dropdownOpen)}
        >
          Tùy chỉnh: {labelMap[range]}
          <span className="arrow">▾</span>
        </button>

        {dropdownOpen && (
          <div className="fb-dropdown-content">
            <div onClick={() => { setRange("week"); setDropdownOpen(false); }}>Tuần</div>
            <div onClick={() => { setRange("month"); setDropdownOpen(false); }}>Tháng</div>
            <div onClick={() => { setRange("year"); setDropdownOpen(false); }}>Năm</div>
            <div onClick={() => { setRange("all"); setDropdownOpen(false); }}>Tất cả</div>
          </div>
        )}
      </div>

      {/* ------------------------------- */}
      {/* 📌 CARDS OVERVIEW */}
      {/* ------------------------------- */}
      <div className="row mt-3">
        <div className="col-sm-6 col-md-6 col-lg-3 mt-3">
          <div className="card">
            <div className="content">
              <div className="row">
                <div className="col-sm-4 text-center">
                  <i className="teal fas fa-shopping-cart fa-2x"></i>
                </div>
                <div className="col-sm-8">
                  <p className="detail-subtitle">Đơn hàng hôm nay</p>
                  <span className="number">{stats.todayOrders}</span>
                </div>
              </div>
              <hr />
              <div className="stats"><i className="fas fa-calendar"></i> Tổng: {stats.totalOrders}</div>
            </div>
          </div>
        </div>

        {/* Revenue */}
        <div className="col-sm-6 col-md-6 col-lg-3 mt-3">
          <div className="card">
            <div className="content">
              <div className="row">
                <div className="col-sm-4 text-center">
                  <i className="olive fas fa-money-bill-alt fa-2x"></i>
                </div>
                <div className="col-sm-8">
                  <p className="detail-subtitle">Doanh thu hôm nay</p>
                  <span className="number">{stats.todayRevenue.toLocaleString()}₫</span>
                </div>
              </div>
              <hr />
              <div className="stats">
                <i className="fas fa-calendar"></i> Tổng: {stats.totalRevenue.toLocaleString()}₫
              </div>
            </div>
          </div>
        </div>

        {/* Visits */}
        <div className="col-sm-6 col-md-6 col-lg-3 mt-3">
          <div className="card">
            <div className="content">
              <div className="row">
                <div className="col-sm-4 text-center">
                  <i className="violet fas fa-eye fa-2x"></i>
                </div>
                <div className="col-sm-8">
                  <p className="detail-subtitle">Lượt truy cập hôm nay</p>
                  <span className="number">{stats.todayVisits}</span>
                </div>
              </div>
              <hr />
              <div className="stats"><i className="fas fa-stopwatch"></i> Tổng: {stats.totalVisits}</div>
            </div>
          </div>
        </div>

        {/* Users */}
        <div className="col-sm-6 col-md-6 col-lg-3 mt-3">
          <div className="card">
            <div className="content">
              <div className="row">
                <div className="col-sm-4 text-center">
                  <i className="orange fas fa-user fa-2x"></i>
                </div>
                <div className="col-sm-8">
                  <p className="detail-subtitle">Tổng người dùng</p>
                  <span className="number">{stats.totalUsers}</span>
                </div>
              </div>
              <hr />
              <div className="stats"><i className="fas fa-user-check"></i> Active users</div>
            </div>
          </div>
        </div>
      </div>

      {/* ------------------------------- */}
      {/* 📌 CHARTS */}
      {/* ------------------------------- */}
      <div className="row mt-4">
        <div className="col-md-6">
          <div className="card">
            <div className="content">
              <h5>📈 Doanh thu ({labelMap[range]})</h5>

              {revenueChart && (
                <p className="text-muted mb-2">
                  Tổng:{" "}
                  <strong>
                    {revenueChart.datasets[0].data.reduce((a, b) => a + b, 0).toLocaleString()}₫
                  </strong>
                </p>
              )}

              <div className="canvas-wrapper">
                {revenueChart ? <Line data={revenueChart} height={180} /> : <p>Đang tải...</p>}
              </div>
            </div>
          </div>
        </div>

        {/* Visitors */}
        <div className="col-md-6">
          <div className="card">
            <div className="content">
              <h5>👁️ Lượt truy cập ({labelMap[range]})</h5>

              {visitorChart && (
                <p className="text-muted mb-2">
                  Tổng:{" "}
                  <strong>
                    {visitorChart.datasets[0].data.reduce((a, b) => a + b, 0).toLocaleString()}
                  </strong>{" "}
                  lượt
                </p>
              )}

              <div className="canvas-wrapper">
                {visitorChart ? <Line data={visitorChart} height={180} /> : <p>Đang tải...</p>}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* ============================= */}
      {/* 📌 CSS STYLE — Dropdown Facebook */}
      {/* ============================= */}
      <style>{`
        .fb-dropdown {
          position: relative;
          display: inline-block;
        }

        .fb-dropbtn {
          background: #fff;
          border: 1px solid #ced0d4;
          padding: 8px 14px;
          border-radius: 6px;
          font-size: 14px;
          cursor: pointer;
          min-width: 180px;
          color: #1c1e21;
          display: flex;
          justify-content: space-between;
          align-items: center;
        }

        .fb-dropbtn:hover {
          background: #f0f2f5;
        }

        .fb-dropdown-content {
          position: absolute;
          background-color: #fff;
          min-width: 180px;
          border-radius: 6px;
          margin-top: 6px;
          box-shadow: 0px 4px 12px rgba(0,0,0,0.15);
          border: 1px solid #ced0d4;
          overflow: hidden;
          z-index: 50;
        }

        .fb-dropdown-content div {
          padding: 10px 14px;
          cursor: pointer;
          font-size: 14px;
        }

        .fb-dropdown-content div:hover {
          background-color: #f0f2f5;
        }

        .arrow {
          font-size: 12px;
          margin-left: 4px;
        }
      `}</style>
    </>
  );
}
