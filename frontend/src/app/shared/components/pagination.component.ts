import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  imports: [],
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.scss'
})
export class PaginationComponent {
  page = input.required<number>();
  pageSize = input.required<number>();
  totalCount = input.required<number>();

  pageChange = output<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount() / this.pageSize()));
  }

  get start(): number {
    return (this.page() - 1) * this.pageSize() + 1;
  }

  get end(): number {
    return Math.min(this.page() * this.pageSize(), this.totalCount());
  }

  goToPage(next: number): void {
    if (next >= 1 && next <= this.totalPages && next !== this.page()) {
      this.pageChange.emit(next);
    }
  }
}
